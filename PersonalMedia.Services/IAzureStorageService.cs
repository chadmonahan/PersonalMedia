namespace PersonalMedia.Services;

public interface IAzureStorageService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName);
    Task<string> UploadFromUrlAsync(string sourceUrl, string fileName);
    Task<bool> DeleteImageAsync(string fileName);
}
