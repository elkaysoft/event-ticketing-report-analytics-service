using Microsoft.AspNetCore.Mvc;

namespace ETS.WebApi.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Ping successful");
        }
    }
}
