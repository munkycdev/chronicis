# Document Storage API Documentation

## Overview

The Document Storage API provides endpoints for uploading, managing, and downloading documents associated with Chronicis worlds. Documents are stored in Azure Blob Storage with metadata tracked in the database.

**Base URL:** `/api/worlds/{worldId}/documents`

---

## Authentication

All endpoints require authentication via Auth0. The user's ID is extracted from the JWT token.

**Authorization Header:**
```
Authorization: Bearer {token}
```

---

## Endpoints

### 1. Request Document Upload

**Endpoint:** `POST /api/worlds/{worldId}/documents/request-upload`

**Description:** Initiates a document upload by generating a time-limited SAS URL for direct client-to-blob upload. This bypasses Azure Functions' 4MB HTTP request limit.

**Permissions:** World member (owner for upload)

**Request Body:**
```json
{
  "fileName": "Campaign Map.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 2048576,
  "description": "Map of Waterdeep for Session 5"
}
```

**Request Parameters:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| fileName | string | Yes | Original filename with extension |
| contentType | string | Yes | MIME type (e.g., "application/pdf") |
| fileSizeBytes | long | Yes | File size in bytes (max 209,715,200) |
| description | string | No | Optional document description |

**Response:** `200 OK`
```json
{
  "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "uploadUrl": "https://stchronicisdev.blob.core.windows.net/chronicis-documents/worlds/.../document.pdf?sv=...",
  "title": "Campaign Map",
  "fileName": "Campaign Map.pdf"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| documentId | Guid | Unique identifier for the document |
| uploadUrl | string | SAS URL for uploading (expires in 15 min) |
| title | string | Auto-generated title (may include "(2)" for duplicates) |
| fileName | string | Sanitized filename |

**Error Responses:**
- `400 Bad Request` - Invalid request (file too large, invalid type, etc.)
- `403 Forbidden` - Not authorized (not world owner)
- `500 Internal Server Error` - Server error

**Example:**
```http
POST /api/worlds/550e8400-e29b-41d4-a716-446655440000/documents/request-upload
Content-Type: application/json

{
  "fileName": "dungeon_map.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 1048576
}
```

---

### 2. Confirm Document Upload

**Endpoint:** `POST /api/worlds/{worldId}/documents/{documentId}/confirm`

**Description:** Confirms that the client successfully uploaded the file to blob storage. Updates document metadata with actual file size and content type from blob.

**Permissions:** World member (original uploader)

**Request Body:**
```json
{
  "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "worldId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "dungeon_map.pdf",
  "title": "dungeon_map",
  "description": null,
  "contentType": "application/pdf",
  "fileSizeBytes": 1048576,
  "blobPath": "worlds/550e8400.../3fa85f64.../dungeon_map.pdf",
  "uploadedAt": "2026-01-02T18:00:00Z",
  "uploadedById": "auth0|123456"
}
```

**Error Responses:**
- `403 Forbidden` - Not authorized
- `404 Not Found` - Document not found or blob missing
- `500 Internal Server Error` - Server error

---

### 3. Get World Documents

**Endpoint:** `GET /api/worlds/{worldId}/documents`

**Description:** Retrieves all documents for a world, sorted by upload date (newest first).

**Permissions:** World member

**Response:** `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "worldId": "550e8400-e29b-41d4-a716-446655440000",
    "fileName": "dungeon_map.pdf",
    "title": "Dungeon Map",
    "description": "Level 1 dungeon layout",
    "contentType": "application/pdf",
    "fileSizeBytes": 1048576,
    "blobPath": "worlds/.../document.pdf",
    "uploadedAt": "2026-01-02T18:00:00Z",
    "uploadedById": "auth0|123456"
  }
]
```

**Error Responses:**
- `403 Forbidden` - Not a world member
- `500 Internal Server Error` - Server error

---

### 4. Get Document Download URL

**Endpoint:** `GET /api/worlds/{worldId}/documents/{documentId}/download`

**Description:** Generates a time-limited SAS URL for downloading the document.

**Permissions:** World member

**Response:** `200 OK`
```json
{
  "downloadUrl": "https://stchronicisdev.blob.core.windows.net/.../document.pdf?sv=...",
  "fileName": "dungeon_map.pdf",
  "contentType": "application/pdf"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| downloadUrl | string | SAS URL for download (expires in 15 min) |
| fileName | string | Original filename for download |
| contentType | string | MIME type |

**Error Responses:**
- `403 Forbidden` - Not a world member
- `404 Not Found` - Document or blob not found
- `500 Internal Server Error` - Server error

**Note:** The SAS URL expires after 15 minutes. Clients should generate a new URL if needed.

---

### 5. Update Document

**Endpoint:** `PUT /api/worlds/{worldId}/documents/{documentId}`

**Description:** Updates document metadata (title and description). Does not modify the file itself.

**Permissions:** World owner only

**Request Body:**
```json
{
  "title": "Updated Dungeon Map",
  "description": "Level 1 and 2 combined"
}
```

**Request Parameters:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| title | string | No | New document title |
| description | string | No | New description (null to clear) |

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "worldId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "dungeon_map.pdf",
  "title": "Updated Dungeon Map",
  "description": "Level 1 and 2 combined",
  "contentType": "application/pdf",
  "fileSizeBytes": 1048576,
  "blobPath": "worlds/.../document.pdf",
  "uploadedAt": "2026-01-02T18:00:00Z",
  "uploadedById": "auth0|123456"
}
```

**Error Responses:**
- `403 Forbidden` - Not world owner
- `404 Not Found` - Document not found
- `500 Internal Server Error` - Server error

---

### 6. Delete Document

**Endpoint:** `DELETE /api/worlds/{worldId}/documents/{documentId}`

**Description:** Deletes a document from both the database and blob storage. This action is permanent.

**Permissions:** World owner only

**Response:** `204 No Content`

**Error Responses:**
- `403 Forbidden` - Not world owner
- `404 Not Found` - Document not found
- `500 Internal Server Error` - Server error

---

## File Upload Flow

The complete upload process follows these steps:

```
┌─────────────────────────────────────────────────────────────────┐
│                          Client                                  │
└─────────────────────────────────────────────────────────────────┘
                │
                │ 1. POST /request-upload
                │    { fileName, contentType, fileSizeBytes }
                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Chronicis API                               │
│  - Validates file (size, type)                                   │
│  - Generates unique title (handles duplicates)                   │
│  - Creates WorldDocument record (pending)                        │
│  - Generates SAS URL with write permissions                      │
└─────────────────────────────────────────────────────────────────┘
                │
                │ Returns: { documentId, uploadUrl, title }
                ▼
┌─────────────────────────────────────────────────────────────────┐
│                          Client                                  │
│  - Uses uploadUrl to PUT file directly to blob                   │
└─────────────────────────────────────────────────────────────────┘
                │
                │ 2. PUT {uploadUrl}
                │    (Binary file content)
                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Azure Blob Storage                            │
│  - Stores file in container                                      │
│  - Returns success/failure                                       │
└─────────────────────────────────────────────────────────────────┘
                │
                │ Upload complete
                ▼
┌─────────────────────────────────────────────────────────────────┐
│                          Client                                  │
│  - Calls confirm endpoint                                        │
└─────────────────────────────────────────────────────────────────┘
                │
                │ 3. POST /confirm
                │    { documentId }
                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Chronicis API                               │
│  - Verifies blob exists                                          │
│  - Updates WorldDocument with actual size/type                   │
│  - Marks as confirmed                                            │
└─────────────────────────────────────────────────────────────────┘
                │
                │ Returns: Complete WorldDocumentDto
                ▼
            Success!
```

---

## Data Models

### WorldDocumentUploadRequestDto
```csharp
public class WorldDocumentUploadRequestDto
{
    public string FileName { get; set; }      // Required, max 255 chars
    public string ContentType { get; set; }   // Required, MIME type
    public long FileSizeBytes { get; set; }   // Required, max 209,715,200
    public string? Description { get; set; }  // Optional, max 1000 chars
}
```

### WorldDocumentUploadResponseDto
```csharp
public class WorldDocumentUploadResponseDto
{
    public Guid DocumentId { get; set; }
    public string UploadUrl { get; set; }  // SAS URL, expires in 15 min
    public string Title { get; set; }
    public string FileName { get; set; }
}
```

### WorldDocumentDto
```csharp
public class WorldDocumentDto
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public string FileName { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string BlobPath { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedById { get; set; }
}
```

### WorldDocumentUpdateDto
```csharp
public class WorldDocumentUpdateDto
{
    public string? Title { get; set; }        // Optional
    public string? Description { get; set; }  // Optional, null clears
}
```

### WorldDocumentDownloadResponseDto
```csharp
public class WorldDocumentDownloadResponseDto
{
    public string DownloadUrl { get; set; }  // SAS URL, expires in 15 min
    public string FileName { get; set; }
    public string ContentType { get; set; }
}
```

---

## Validation Rules

### File Size
- **Minimum:** 1 byte
- **Maximum:** 209,715,200 bytes (200 MB)

### File Types (Extensions)
**Allowed:**
- Documents: `.pdf`, `.docx`, `.txt`, `.md`
- Spreadsheets: `.xlsx`
- Presentations: `.pptx`
- Images: `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`

**Blocked:** All other extensions

### File Name
- Sanitized to remove invalid characters
- Limited to 200 characters after sanitization
- Preserves extension

### Title
- Auto-generated from filename (without extension)
- Automatically renamed if duplicate: "Title (2)", "Title (3)", etc.
- Can be manually edited after upload

---

## Security

### Authentication
- All endpoints require valid Auth0 JWT token
- User ID extracted from `sub` claim

### Authorization
- **Upload/Delete/Update:** World owner only
- **Download/List:** All world members

### SAS Tokens
- **Upload:** Write-only, 15-minute expiration, scoped to specific blob
- **Download:** Read-only, 15-minute expiration, scoped to specific blob
- Tokens are generated per-request, not stored

### Blob Storage
- Container: `chronicis-documents` (private access)
- Blob path pattern: `worlds/{worldId}/documents/{documentId}/{sanitized-filename}`
- Direct access requires SAS token

---

## Error Handling

### Client Errors (4xx)

**400 Bad Request**
```json
{
  "error": "File size exceeds maximum allowed size of 200 MB"
}
```

**403 Forbidden**
```json
{
  "error": "Only world owners can upload documents"
}
```

**404 Not Found**
```json
{
  "error": "Document not found"
}
```

### Server Errors (5xx)

**500 Internal Server Error**
```json
{
  "error": "Failed to generate upload URL"
}
```

---

## Rate Limiting

Currently no rate limiting is enforced. Recommended limits for future:
- Upload requests: 10 per minute per user
- Download URL generation: 60 per minute per user

---

## Examples

### Complete Upload Example (JavaScript)

```javascript
// Step 1: Request upload
const requestResponse = await fetch('/api/worlds/{worldId}/documents/request-upload', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    fileName: file.name,
    contentType: file.type,
    fileSizeBytes: file.size,
    description: "My document"
  })
});

const { documentId, uploadUrl } = await requestResponse.json();

// Step 2: Upload to blob storage
const uploadResponse = await fetch(uploadUrl, {
  method: 'PUT',
  headers: {
    'x-ms-blob-type': 'BlockBlob',
    'Content-Type': file.type
  },
  body: file
});

if (!uploadResponse.ok) {
  throw new Error('Upload failed');
}

// Step 3: Confirm upload
const confirmResponse = await fetch(
  `/api/worlds/{worldId}/documents/${documentId}/confirm`,
  {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ documentId })
  }
);

const document = await confirmResponse.json();
console.log('Upload complete:', document);
```

### Download Example (JavaScript)

```javascript
// Get download URL
const response = await fetch(
  `/api/worlds/{worldId}/documents/{documentId}/download`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
);

const { downloadUrl, fileName } = await response.json();

// Open in new tab (browser handles download)
window.open(downloadUrl, '_blank');
```

---

## Changelog

### v1.0 (January 2026)
- Initial release
- Upload, download, list, update, delete endpoints
- SAS token-based security
- Auto-rename for duplicates
- File type and size validation

---

## Support

For API issues or questions:
- Check error responses for specific error messages
- Review validation rules above
- Contact support via Chronicis feedback system

---

**Last Updated:** January 2, 2026
