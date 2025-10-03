using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Recipe_Sharing_Platform_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/[controller]")]
    public class HelloController : ControllerBase
    {
        [HttpGet("SayHello")]
        public string SayHello()
        {
            return "Hello from HelloController!";
        }
    }
}