using Amazon.S3;
using Amazon.S3.Model;

namespace ProductService.Services;

public sealed class S3ImageStorage
{
    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _region;
    private readonly string? _publicEndpoint;

    public S3ImageStorage(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS_S3_BUCKET"] ?? string.Empty;
        _region = configuration["AWS_REGION"] ?? "us-east-1";
        _publicEndpoint = configuration["AWS_S3_PUBLIC_ENDPOINT"];
    }

    public async Task<List<string>> UploadImagesAsync(
        IReadOnlyCollection<IFormFile> images,
        CancellationToken cancellationToken)
    {
        if (images.Count == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            throw new InvalidOperationException("AWS_S3_BUCKET must be configured to upload product images.");
        }

        var uploadedKeys = new List<string>();
        var urls = new List<string>();

        try
        {
            foreach (var image in images)
            {
                var key = $"products/{Guid.NewGuid():N}/{Guid.NewGuid():N}{GetExtension(image.ContentType)}";
                await using var stream = image.OpenReadStream();

                await _s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = image.ContentType
                }, cancellationToken);

                uploadedKeys.Add(key);
                urls.Add(string.IsNullOrWhiteSpace(_publicEndpoint)
                    ? $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}"
                    : $"{_publicEndpoint.TrimEnd('/')}/{_bucketName}/{key}");
            }
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try
                {
                    await _s3Client.DeleteObjectAsync(_bucketName, key, CancellationToken.None);
                }
                catch
                {
                    // Preserve the original upload failure.
                }
            }

            throw;
        }

        return urls;
    }

    private static string GetExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        _ => throw new ArgumentException("Unsupported image content type.", nameof(contentType))
    };
}
