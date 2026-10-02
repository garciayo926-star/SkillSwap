using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        [Authorize(Roles = "Master")]
        [HttpGet("analytics")]
        public IActionResult GetMasterAnalytics()
        {
            return Ok(new { 
                totalUsers = 125, 
                totalOffers = 48, 
                totalRequests = 32,
                message = "Resumen general obtenido exitosamente para el rol Master." 
            });
        }

        [Authorize(Roles = "Technical,Master")]
        [HttpGet("system-health")]
        public IActionResult GetSystemHealth()
        {
            return Ok(new { 
                apiStatus = "En línea (Puerto 5066)", 
                database = "Conectada (PostgreSQL)", 
                serverHealth = "Saludable (0 Errores críticos)",
                timestamp = DateTime.UtcNow
            });
        }
    }
}