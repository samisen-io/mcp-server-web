using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Blob Storage
var blobConnectionString = builder.Configuration["AzureBlobStorage:ConnectionString"];

if(blobConnectionString == null || blobConnectionString =="")
  blobConnectionString = builder.Configuration["AzureBlobStorageConnectionString"];

var containerName = builder.Configuration["AzureBlobStorage:ContainerName"];

if(containerName == null || containerName == "")
  containerName = builder.Configuration["AzureBlobStorageContainerName"];

if (string.IsNullOrEmpty(blobConnectionString) || string.IsNullOrEmpty(containerName))
{
    throw new InvalidOperationException("Azure Blob Storage connection string and container name must be configured in appsettings.");
}

builder.Services.AddSingleton(sp =>
{
    var blobServiceClient = new BlobServiceClient(blobConnectionString);
    return blobServiceClient.GetBlobContainerClient(containerName);
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
var app = builder.Build();

app.MapMcp();

app.Run(); // Use Kestrel configuration from launchSettings.json

