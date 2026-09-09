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
            [".webp"] = "image/webp"
        };

        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryImageService> _logger;

        public CloudinaryImageService(IConfiguration configuration, ILogger<CloudinaryImageService> logger)
        {
            _logger = logger;
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
                throw new InvalidOperationException("Cloudinary ayarları eksik.");

            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        }

        public async Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(file, cancellationToken);
            _logger.LogInformation("Cloudinary görsel yükleme başladı. Boyut: {FileSize}, Tip: {ContentType}", file.Length, file.ContentType);

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(Path.GetFileName(file.FileName), stream),
                Folder = "staj-images",
                PublicId = $"{Guid.NewGuid():N}",
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (result.Error is not null)
            {
                _logger.LogError("Cloudinary görsel yükleme başarısız. {ErrorMessage}", result.Error.Message);
                throw new InvalidOperationException("Görsel depolama servisi şu anda kullanılamıyor.");
            }

            if (result.SecureUrl is null)
                throw new InvalidOperationException("Görsel depolama servisi geçerli bir URL döndürmedi.");

            return result.SecureUrl.AbsoluteUri;
        }

        private static async Task ValidateAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("Görsel dosyası boş olamaz.");
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Görsel en fazla 5 MB olabilir.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedTypes.TryGetValue(extension, out var expectedContentType)
                || !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Sadece JPG, JPEG, PNG veya WEBP görseller kabul edilir.");

            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (!IsValidImageSignature(extension, header, read))
                throw new ArgumentException("Dosya içeriği seçilen görsel türüyle eşleşmiyor.");
        }

        private static bool IsValidImageSignature(string extension, byte[] header, int length)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => length >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => length >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
                _ => false
            };
        }
    }
}
