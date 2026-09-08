using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAJ.Services;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly ImageStorageService _storage;
        public ImagesController(ImageStorageService storage) => _storage = storage;

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null) return BadRequest("Görsel dosyası gönderilmelidir.");
            try
            {
                var id = await _storage.AddAsync(file, cancellationToken);
                return Ok(new { id, url = $"/api/Images/{id}" });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            var image = await _storage.GetAsync(id, cancellationToken);
            return image is null ? NotFound() : File(image.Value.Data, image.Value.ContentType);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _storage.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
