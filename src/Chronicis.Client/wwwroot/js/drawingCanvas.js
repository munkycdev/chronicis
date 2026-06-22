// ================================================
// Drawing Canvas JS Interop for Handwritten Notes
// ================================================
// Handles pointer events (stylus/touch/mouse),
// pressure-sensitive stroke rendering, undo/redo,
// pen/eraser tools, and PNG export via canvas.toBlob().
// ================================================

const canvasInstances = new Map();

const COLORS = ['#000000', '#FF0000', '#0000FF', '#008000', '#FF8C00', '#800080'];

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function pressureToWidth(pressure) {
    if (!pressure || pressure === 0) return 2;
    return clamp(pressure * 8, 1, 8);
}

class DrawingCanvasInstance {
    constructor(canvasElement, dotNetHelper) {
        this.canvas = canvasElement;
        this.ctx = canvasElement.getContext('2d');
        this.dotNetHelper = dotNetHelper;
        this.strokes = [];
        this.redoStack = [];
        this.currentStroke = null;
        this.currentColor = COLORS[0];
        this.tool = 'pen'; // 'pen' or 'eraser'
        this.isDrawing = false;

        this._setupContext();
        this._attachEvents();
    }

    _setupContext() {
        this.ctx.lineCap = 'round';
        this.ctx.lineJoin = 'round';
    }

    _getCanvasPoint(e) {
        const rect = this.canvas.getBoundingClientRect();
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;
        return {
            x: e.offsetX * scaleX,
            y: e.offsetY * scaleY,
            pressure: e.pressure
        };
    }

    _attachEvents() {
        this.canvas.addEventListener('pointerdown', this._onPointerDown.bind(this));
        this.canvas.addEventListener('pointermove', this._onPointerMove.bind(this));
        this.canvas.addEventListener('pointerup', this._onPointerUp.bind(this));
        this.canvas.addEventListener('pointerleave', this._onPointerUp.bind(this));
        this.canvas.addEventListener('pointercancel', this._onPointerUp.bind(this));
        // Prevent default touch scrolling on the canvas
        this.canvas.style.touchAction = 'none';
    }

    _onPointerDown(e) {
        e.preventDefault();
        this.isDrawing = true;

        if (this.tool === 'eraser') {
            const pt = this._getCanvasPoint(e);
            this._eraseAtPoint(pt.x, pt.y);
            return;
        }

        const point = this._getCanvasPoint(e);

        this.currentStroke = {
            points: [point],
            color: this.currentColor
        };
    }

    _onPointerMove(e) {
        if (!this.isDrawing) return;
        e.preventDefault();

        if (this.tool === 'eraser') {
            const pt = this._getCanvasPoint(e);
            this._eraseAtPoint(pt.x, pt.y);
            return;
        }

        if (!this.currentStroke) return;

        const point = this._getCanvasPoint(e);

        this.currentStroke.points.push(point);
        this._renderLastSegment();
    }

    _onPointerUp(e) {
        if (!this.isDrawing) return;
        this.isDrawing = false;

        if (this.tool === 'eraser') return;

        if (this.currentStroke && this.currentStroke.points.length > 0) {
            this.strokes.push(this.currentStroke);
            this.redoStack = [];
            this._notifyStrokeCountChanged();
        }
        this.currentStroke = null;
    }

    _renderLastSegment() {
        const points = this.currentStroke.points;
        if (points.length < 2) return;

        const prev = points[points.length - 2];
        const curr = points[points.length - 1];

        this.ctx.beginPath();
        this.ctx.strokeStyle = this.currentStroke.color;
        this.ctx.lineWidth = pressureToWidth(curr.pressure);
        this.ctx.moveTo(prev.x, prev.y);
        this.ctx.lineTo(curr.x, curr.y);
        this.ctx.stroke();
    }

    _eraseAtPoint(x, y) {
        const hitRadius = 10;
        let erased = false;

        for (let i = this.strokes.length - 1; i >= 0; i--) {
            const stroke = this.strokes[i];
            for (const point of stroke.points) {
                const dx = point.x - x;
                const dy = point.y - y;
                if (dx * dx + dy * dy <= hitRadius * hitRadius) {
                    this.strokes.splice(i, 1);
                    this.redoStack = [];
                    erased = true;
                    break;
                }
            }
            if (erased) break;
        }

        if (erased) {
            this._redrawAll();
            this._notifyStrokeCountChanged();
        }
    }

    _redrawAll() {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        for (const stroke of this.strokes) {
            this._renderStroke(stroke);
        }
    }

    _renderStroke(stroke) {
        if (stroke.points.length === 0) return;

        if (stroke.points.length === 1) {
            const p = stroke.points[0];
            this.ctx.beginPath();
            this.ctx.fillStyle = stroke.color;
            const r = pressureToWidth(p.pressure) / 2;
            this.ctx.arc(p.x, p.y, r, 0, Math.PI * 2);
            this.ctx.fill();
            return;
        }

        for (let i = 1; i < stroke.points.length; i++) {
            const prev = stroke.points[i - 1];
            const curr = stroke.points[i];
            this.ctx.beginPath();
            this.ctx.strokeStyle = stroke.color;
            this.ctx.lineWidth = pressureToWidth(curr.pressure);
            this.ctx.lineCap = 'round';
            this.ctx.lineJoin = 'round';
            this.ctx.moveTo(prev.x, prev.y);
            this.ctx.lineTo(curr.x, curr.y);
            this.ctx.stroke();
        }
    }

    _notifyStrokeCountChanged() {
        if (this.dotNetHelper) {
            this.dotNetHelper.invokeMethodAsync('OnStrokeCountChanged', this.strokes.length);
        }
    }

    undo() {
        if (this.strokes.length === 0) return;
        const stroke = this.strokes.pop();
        this.redoStack.push(stroke);
        this._redrawAll();
        this._notifyStrokeCountChanged();
    }

    redo() {
        if (this.redoStack.length === 0) return;
        const stroke = this.redoStack.pop();
        this.strokes.push(stroke);
        this._redrawAll();
        this._notifyStrokeCountChanged();
    }

    setColor(color) {
        this.currentColor = color;
    }

    setTool(tool) {
        this.tool = tool;
    }

    getStrokeCount() {
        return this.strokes.length;
    }

    clear() {
        this.strokes = [];
        this.redoStack = [];
        this.currentStroke = null;
        this._redrawAll();
        this._notifyStrokeCountChanged();
    }

    async exportPng() {
        return new Promise((resolve, reject) => {
            this.canvas.toBlob((blob) => {
                if (!blob) {
                    reject(new Error('Failed to export canvas as PNG'));
                    return;
                }
                blob.arrayBuffer().then(buffer => {
                    resolve(new Uint8Array(buffer));
                }).catch(reject);
            }, 'image/png');
        });
    }

    dispose() {
        this.canvas.removeEventListener('pointerdown', this._onPointerDown);
        this.canvas.removeEventListener('pointermove', this._onPointerMove);
        this.canvas.removeEventListener('pointerup', this._onPointerUp);
        this.canvas.removeEventListener('pointerleave', this._onPointerUp);
        this.canvas.removeEventListener('pointercancel', this._onPointerUp);
    }
}

/**
 * Initialize a drawing canvas for a given element.
 * Called from Blazor JS interop.
 */
window.drawingCanvasInitialize = function (canvasElementId, dotNetHelper) {
    const canvas = document.getElementById(canvasElementId);
    if (!canvas) return false;

    const instance = new DrawingCanvasInstance(canvas, dotNetHelper);
    canvasInstances.set(canvasElementId, instance);
    return true;
};

/**
 * Undo the last stroke.
 */
window.drawingCanvasUndo = function (canvasElementId) {
    const instance = canvasInstances.get(canvasElementId);
    if (instance) instance.undo();
};

/**
 * Redo the last undone stroke.
 */
window.drawingCanvasRedo = function (canvasElementId) {
    const instance = canvasInstances.get(canvasElementId);
    if (instance) instance.redo();
};

/**
 * Export the canvas content as PNG bytes.
 * Returns a Uint8Array via promise.
 */
window.drawingCanvasExportPng = async function (canvasElementId) {
    const instance = canvasInstances.get(canvasElementId);
    if (!instance) return null;
    return await instance.exportPng();
};

/**
 * Set the active drawing color.
 */
window.drawingCanvasSetColor = function (canvasElementId, color) {
    const instance = canvasInstances.get(canvasElementId);
    if (instance) instance.setColor(color);
};

/**
 * Set the active tool ('pen' or 'eraser').
 */
window.drawingCanvasSetTool = function (canvasElementId, tool) {
    const instance = canvasInstances.get(canvasElementId);
    if (instance) instance.setTool(tool);
};

/**
 * Get the current stroke count.
 */
window.drawingCanvasGetStrokeCount = function (canvasElementId) {
    const instance = canvasInstances.get(canvasElementId);
    return instance ? instance.getStrokeCount() : 0;
};

/**
 * Clear all strokes from the canvas.
 */
window.drawingCanvasClear = function (canvasElementId) {
    const instance = canvasInstances.get(canvasElementId);
    if (instance) instance.clear();
};

/**
 * Dispose a canvas instance and clean up resources.
 */
window.drawingCanvasDispose = function (canvasElementId) {
    const instance = canvasInstances.get(canvasElementId);
    if (instance) {
        instance.dispose();
        canvasInstances.delete(canvasElementId);
    }
};

/**
 * Fetch an image from a URL and return it as a byte array.
 * Used for transcribing an already-saved handwritten note from the tab view.
 */
window.fetchImageAsBytes = async function (url) {
    try {
        const response = await fetch(url);
        if (!response.ok) return null;
        const buffer = await response.arrayBuffer();
        return new Uint8Array(buffer);
    } catch {
        return null;
    }
};
