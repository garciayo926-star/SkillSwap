using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangesController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public ExchangesController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Exchanges/matches/{studentId} (Motor de Coincidencias Automáticas)
        [HttpGet("matches/{studentId}")]
        public async Task<IActionResult> GetMatchesForStudent(int studentId)
        {
            // 1. VALIDACIÓN DE ID: Comprobar si el estudiante realmente existe
            var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
            if (!studentExists)
            {
                return NotFound(new { message = $"El estudiante con ID {studentId} no existe en el sistema." });
            }

            // 2. Obtener las habilidades que este estudiante OFRECE
            var myOffers = await _context.Offers
                .Where(o => o.StudentId == studentId && o.IsActive)
                .Select(o => o.SkillId)
                .ToListAsync();

            // 3. Obtener las habilidades que este estudiante QUIERE APRENDER (Requests)
            var myRequests = await _context.Requests
                .Where(r => r.StudentId == studentId && r.IsActive)
                .Select(r => r.SkillId)
                .ToListAsync();

            // 4. Buscar estudiantes compatibles (Match Cruzado)
            var matches = await _context.Students
                .Include(s => s.User)
                .Where(s => s.Id != studentId)
                .Select(otherStudent => new {
                    StudentId = otherStudent.Id,
                    FullName = otherStudent.User.FirstName + " " + otherStudent.User.LastName,
                    otherStudent.Bio,
                    OfferedSkillsToMe = otherStudent.Offers
                        .Where(o => myRequests.Contains(o.SkillId) && o.IsActive)
                        .Select(o => o.Skill.Name).ToList(),
                    WantedSkillsFromMe = otherStudent.Requests
                        .Where(r => myOffers.Contains(r.SkillId) && r.IsActive)
                        .Select(r => r.Skill.Name).ToList()
                })
                .Where(m => m.OfferedSkillsToMe.Any() && m.WantedSkillsFromMe.Any())
                .ToListAsync();

            return Ok(matches);
        }

        // POST: api/Exchanges (Proponer un intercambio formal)
        [HttpPost]
        public async Task<ActionResult<Exchange>> PostExchange(Exchange exchange)
        {
            // VALIDACIÓN DE IDs: Verificar que ambos estudiantes existan
            var initiatorExists = await _context.Students.AnyAsync(s => s.Id == exchange.InitiatorStudentId);
            var receiverExists = await _context.Students.AnyAsync(s => s.Id == exchange.ReceiverStudentId);

            if (!initiatorExists || !receiverExists)
            {
                return BadRequest(new { message = "Uno de los IDs de los estudiantes participantes no es válido o no existe." });
            }

            if (exchange.InitiatorStudentId == exchange.ReceiverStudentId)
            {
                return BadRequest(new { message = "Un estudiante no puede realizar un intercambio consigo mismo." });
            }

            exchange.Status = "Pendiente";
            exchange.DateProposed = DateTime.UtcNow;

            _context.Exchanges.Add(exchange);
            await _context.SaveChangesAsync();

            return Ok(exchange);
        }
    }
}