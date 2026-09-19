# MCP Server — Azure Blob Storage tools

An ASP.NET Core [MCP](https://modelcontextprotocol.io) server (**net8.0**) that gives any MCP client
a small, typed file API over an Azure Blob container: list, upload, download, delete, exists.

Built with `ModelContextProtocol.AspNetCore` using HTTP transport and
`WithToolsFromAssembly()`, so tools are declared once with attributes and discovered automatically.

## Why

Agents reason well and handle file plumbing badly. Putting blob storage behind described, typed
tools means the model asks for "list blobs with prefix `reports/`" instead of improvising SDK calls
— and every operation is auditable in the transcript.

## Tools

| Tool | Description |
|---|---|
| `ListBlobs(prefix?)` | Lists blobs in the container, optionally filtered by folder prefix, with size and modified time |
| `UploadBlob(blobName, content, contentType?)` | Uploads text content, overwriting if the blob exists |
| `DownloadBlob(blobName)` | Returns blob content as text |
| `DeleteBlob(blobName)` | Deletes a blob |
| `BlobExists(blobName)` | Existence check before expensive operations |
| `EchoTools` | Echo/echo-typed tools used to smoke-test a client connection end to end |

## Configuration

The server refuses to start without storage configuration — deliberate, so a misconfigured
deployment fails loudly instead of silently writing nowhere.

```jsonc
// appsettings.json (values intentionally empty in the repo — set them locally or via env vars)
"AzureBlobStorage": { "ConnectionString": "", "ContainerName": "" }
```

Environment variables accepted as an alternative: `AzureBlobStorageConnectionString`,
`AzureBlobStorageContainerName`. **No credentials are committed** — the connection string is read
from configuration at startup only.

## Run

```bash
dotnet restore
dotnet run          # port comes from Properties/launchSettings.json; MCP is mapped via app.MapMcp()
```

Then point an MCP client at the server's MCP endpoint.

## Design notes

- `BlobTools` is a `[McpServerToolType]` taking an injected `BlobContainerClient` — the tool layer
  holds no storage logic of its own.
- Tool failures return readable text (`Error: Blob 'x' does not exist.`) rather than throwing, so an
  agent can recover instead of aborting the turn.
- `WithToolsFromAssembly()` means adding a tool is adding an attributed method.

## Limitations and next steps

- **No automated tests yet.** The next step is an integration test against Azurite (the Azure Storage
  emulator), which would make the tool contracts regression-proof.
- Text content only — binary upload/download needs a streaming path with size limits.
- No pagination on `ListBlobs`; large containers will need continuation tokens.
- No auth on the MCP endpoint: run it behind your own gateway before exposing it beyond localhost.
