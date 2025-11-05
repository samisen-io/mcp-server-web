using ModelContextProtocol.Server;
using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;

namespace MCPServer.Tools;

[McpServerToolType]
public class BlobTool(BlobContainerClient containerClient)
{
    [McpServerTool, Description("Lists all blobs/files in the container. Optionally filter by prefix (folder path).")]
    public async Task<string> ListBlobs(string? prefix = null)
    {
        try
        {
            var blobs = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                var size = blobItem.Properties.ContentLength.HasValue
                    ? $"{blobItem.Properties.ContentLength.Value / 1024.0:F2} KB"
                    : "Unknown";
                var lastModified = blobItem.Properties.LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
                blobs.Add($"{blobItem.Name} | Size: {size} | Modified: {lastModified}");
            }

            if (blobs.Count == 0)
            {
                return prefix == null
                    ? "No blobs found in container."
                    : $"No blobs found with prefix '{prefix}'.";
            }

            return $"Found {blobs.Count} blob(s):\n" + string.Join("\n", blobs);
        }
        catch (Exception ex)
        {
            return $"Error listing blobs: {ex.Message}";
        }
    }

    [McpServerTool, Description("Uploads content to a blob/file. Overwrites if the blob already exists.")]
    public async Task<string> UploadBlob(string blobName, string content, string? contentType = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return "Error: Blob name cannot be empty.";
            }

            var blobClient = containerClient.GetBlobClient(blobName);
            var bytes = Encoding.UTF8.GetBytes(content);

            using var stream = new MemoryStream(bytes);
            var options = new BlobUploadOptions();

            if (!string.IsNullOrEmpty(contentType))
            {
                options.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            }

            await blobClient.UploadAsync(stream, options);

            return $"Successfully uploaded blob '{blobName}' ({bytes.Length} bytes).";
        }
        catch (Exception ex)
        {
            return $"Error uploading blob: {ex.Message}";
        }
    }

    [McpServerTool, Description("Downloads and returns the content of a blob/file as text.")]
    public async Task<string> DownloadBlob(string blobName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return "Error: Blob name cannot be empty.";
            }

            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return $"Error: Blob '{blobName}' does not exist.";
            }

            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();

            return $"Content of '{blobName}':\n\n{content}";
        }
        catch (Exception ex)
        {
            return $"Error downloading blob: {ex.Message}";
        }
    }

    [McpServerTool, Description("Deletes a blob/file from the container.")]
    public async Task<string> DeleteBlob(string blobName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return "Error: Blob name cannot be empty.";
            }

            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                return $"Successfully deleted blob '{blobName}'.";
            }
            else
            {
                return $"Blob '{blobName}' does not exist.";
            }
        }
        catch (Exception ex)
        {
            return $"Error deleting blob: {ex.Message}";
        }
    }

    [McpServerTool, Description("Checks if a blob/file exists in the container.")]
    public async Task<string> BlobExists(string blobName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return "Error: Blob name cannot be empty.";
            }

            var blobClient = containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync();

            return exists.Value
                ? $"Blob '{blobName}' exists."
                : $"Blob '{blobName}' does not exist.";
        }
        catch (Exception ex)
        {
            return $"Error checking blob existence: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets properties and metadata of a blob/file (size, content type, last modified, etc.).")]
    public async Task<string> GetBlobProperties(string blobName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return "Error: Blob name cannot be empty.";
            }

            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return $"Error: Blob '{blobName}' does not exist.";
            }

            var properties = await blobClient.GetPropertiesAsync();
            var props = properties.Value;

            var info = new StringBuilder();
            info.AppendLine($"Blob Properties for '{blobName}':");
            info.AppendLine($"  Size: {props.ContentLength / 1024.0:F2} KB ({props.ContentLength} bytes)");
            info.AppendLine($"  Content Type: {props.ContentType}");
            info.AppendLine($"  Last Modified: {props.LastModified:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"  Created On: {props.CreatedOn:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"  ETag: {props.ETag}");

            if (props.Metadata.Count > 0)
            {
                info.AppendLine($"  Metadata:");
                foreach (var metadata in props.Metadata)
                {
                    info.AppendLine($"    {metadata.Key}: {metadata.Value}");
                }
            }

            return info.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting blob properties: {ex.Message}";
        }
    }

    [McpServerTool, Description("Copies a blob to a new location within the same container.")]
    public async Task<string> CopyBlob(string sourceBlobName, string destinationBlobName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceBlobName) || string.IsNullOrWhiteSpace(destinationBlobName))
            {
                return "Error: Source and destination blob names cannot be empty.";
            }

            var sourceBlobClient = containerClient.GetBlobClient(sourceBlobName);

            if (!await sourceBlobClient.ExistsAsync())
            {
                return $"Error: Source blob '{sourceBlobName}' does not exist.";
            }

            var destinationBlobClient = containerClient.GetBlobClient(destinationBlobName);
            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

            return $"Successfully copied '{sourceBlobName}' to '{destinationBlobName}'.";
        }
        catch (Exception ex)
        {
            return $"Error copying blob: {ex.Message}";
        }
    }
}