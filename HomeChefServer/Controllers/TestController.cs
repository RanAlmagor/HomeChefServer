using Microsoft.AspNetCore.Mvc;

namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("✅ השרת פעיל ומוכן לעבודה");
        }
    }
}
