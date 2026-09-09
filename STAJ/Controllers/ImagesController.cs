using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAJ.Services;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class ImagesController : ControllerBase
    {
        private readonly CloudinaryImageService _imageService;

        public ImagesController(CloudinaryImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null)
                return BadRequest("Görsel dosyası gönderilmelidir.");

            try
            {
                var url = await _imageService.UploadAsync(file, cancellationToken);
                return Ok(new { url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
            }
        }
    }
}