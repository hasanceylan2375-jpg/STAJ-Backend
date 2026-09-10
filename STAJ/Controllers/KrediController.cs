using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAJ.Models;
using STAJ.Services;
namespace STAJ.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KrediController : ControllerBase
{
 [HttpPost("odeme-plani")]
 public ActionResult<KrediOdemePlanResponse> OdemePlani([FromBody]KrediOdemePlanRequest request){try{return Ok(new KrediOdemePlanService().Hesapla(request));}catch(ArgumentException ex){return BadRequest(new{mesaj=ex.Message});}}
}
