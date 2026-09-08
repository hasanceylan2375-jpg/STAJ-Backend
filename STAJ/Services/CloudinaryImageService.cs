using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace STAJ.Services
{
    public class CloudinaryImageService
    {
        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".svg"] = "image/svg+xml"
        };

        private readonly Cloudinary _cloudinary;

        public CloudinaryImageService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
                throw new InvalidOperationException("Cloudinary ayarları eksik. CloudName, ApiKey ve ApiSecret tanımlanmalıdır.");

            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        }

        public async Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            Validate(file);

            await using var stream = file.OpenReadStream();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var publicId = $"{Guid.NewGuid():N}";

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "staj-images",
                PublicId = publicId,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (result.Error is not null)
                throw new InvalidOperationException($"Cloudinary görsel yükleme hatası: {result.Error.Message}");

            if (result.SecureUrl is null)
                throw new InvalidOperationException("Cloudinary güvenli görsel URL'si döndürmedi.");

            return result.SecureUrl.AbsoluteUri;
        }

        private static void Validate(IFormFile file)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("Görsel dosyası boş olamaz.");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Görsel en fazla 5 MB olabilir.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedTypes.TryGetValue(extension, out var expectedContentType)
                || !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Sadece JPG, JPEG, PNG, WebP veya SVG görseller kabul edilir.");
            }
        }
    }
}
