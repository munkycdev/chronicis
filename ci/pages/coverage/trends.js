(async function () {
  // Guard: if the script is injected/loaded multiple times, only run once.
  if (window.__coverageTrendsInitialized) return;
  window.__coverageTrendsInitialized = true;

  const errorId = "coverage-trends-error";

  function showError(msg) {
    if (document.getElementById(errorId)) return;
    const p = document.createElement("p");
    p.id = errorId;
    p.className = "muted";
    p.textContent = msg;
    document.body.appendChild(p);
  }

  try {
    const res = await fetch("./history.json", { cache: "no-store" });
    if (!res.ok) throw new Error(`history.json fetch failed: ${res.status}`);
    const data = await res.json();

    const points = (data.points || []).slice().reverse();

    // Clear table body before writing (prevents growth on repeat runs)
    const tbody = document.querySelector("#runs tbody");
    if (!tbody) throw new Error("Could not find #runs tbody");
    tbody.innerHTML = "";

    for (const p of points.slice(0, 25)) {
      const tr = document.createElement("tr");
      const shaShort = (p.sha || "").slice(0, 8);
      tr.innerHTML = `
        <td>${p.date || ""}</td>
        <td>${p.linePct ?? ""}</td>
        <td>${p.branchPct ?? ""}</td>
        <td><code title="${p.sha || ""}">${shaShort}</code></td>
      `;
      tbody.appendChild(tr);
    }

    // Chart rendering: destroy an existing chart if this runs twice anyway
    const canvas = document.getElementById("chart");
    if (!canvas) throw new Error("Could not find #chart canvas");

    const labels = points.map(p => p.date);
    const line = points.map(p => p.linePct);
    const branch = points.map(p => p.branchPct);

    if (window.__coverageTrendChart) {
      window.__coverageTrendChart.destroy();
      window.__coverageTrendChart = null;
    }

    window.__coverageTrendChart = new Chart(canvas, {
      type: "line",
      data: {
        labels,
        datasets: [
          { label: "Line %", data: line, spanGaps: true },
          { label: "Branch %", data: branch, spanGaps: true }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,

        // Stop animation loop
        animation: false,

        // Prevent resize thrash
        resizeDelay: 200,

        scales: {
          y: { min: 0, max: 100 }
        },
        plugins: {
          legend: { position: "top" }
        }
      }
    });
  } catch (err) {
    console.error(err);
    showError("Failed to render trends. Check console for details.");
  }
})();
