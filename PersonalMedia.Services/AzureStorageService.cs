using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace PersonalMedia.Services;

public class AzureStorageService : IAzureStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureStorage:ContainerName"] ?? "personal-media";

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        await blobClient.UploadAsync(imageStream, new BlobHttpHeaders
        {
            ContentType = "image/jpeg"
        });

        return blobClient.Uri.ToString();
    }

    public async Task<string> UploadFromUrlAsync(string sourceUrl, string fileName)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(sourceUrl);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        return await UploadImageAsync(stream, fileName);
    }

    public async Task<bool> DeleteImageAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        return await blobClient.DeleteIfExistsAsync();
    }
}
