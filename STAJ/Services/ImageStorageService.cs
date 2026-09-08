using System.Text.Json;

namespace STAJ.Services
{
    public class ImageStorageService
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        public ImageStorageService(IWebHostEnvironment environment)
        {
            _filePath = Path.Combine(environment.ContentRootPath, "Data", "images.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "{}");
        }

        public async Task<string> AddAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file.Length == 0) throw new ArgumentException("Görsel dosyası boş olamaz.");
            if (file.Length > 5 * 1024 * 1024) throw new ArgumentException("Görsel en fazla 5 MB olabilir.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new Dictionary<string, string>
            {
                [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png",
                [".webp"] = "image/webp", [".svg"] = "image/svg+xml"
            };
            if (!allowed.TryGetValue(extension, out var contentType) || !string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Sadece JPG, JPEG, PNG, WebP veya SVG görseller kabul edilir.");

            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            var item = new StoredImage(Guid.NewGuid().ToString("N"), contentType, Convert.ToBase64String(stream.ToArray()));

            await _lock.WaitAsync(cancellationToken);
            try
            {
                var images = await ReadAsync(cancellationToken);
                images[item.Id] = item;
                await WriteAsync(images, cancellationToken);
            }
            finally { _lock.Release(); }

            return item.Id;
        }

        public async Task<(string ContentType, byte[] Data)?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var images = await ReadAsync(cancellationToken);
                if (!images.TryGetValue(id, out var image)) return null;
                return (image.ContentType, Convert.FromBase64String(image.Data));
            }
            finally { _lock.Release(); }
        }

        public async Task DeleteAsync(string? id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var images = await ReadAsync(cancellationToken);
                if (images.Remove(id)) await WriteAsync(images, cancellationToken);
            }
            finally { _lock.Release(); }
        }

        private async Task<Dictionary<string, StoredImage>> ReadAsync(CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, StoredImage>>(stream, _jsonOptions, cancellationToken) ?? [];
        }

        private async Task WriteAsync(Dictionary<string, StoredImage> images, CancellationToken cancellationToken)
        {
            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(images, _jsonOptions), cancellationToken);
            File.Move(tempPath, _filePath, true);
        }

        private sealed record StoredImage(string Id, string ContentType, string Data);
    }
}
