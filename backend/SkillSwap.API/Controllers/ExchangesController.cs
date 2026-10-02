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

        // GET: api/Exchanges (Listar todos los intercambios)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Exchange>>> GetExchanges()
        {
            return await _context.Exchanges
                .Include(e => e.InitiatorStudent).ThenInclude(s => s.User)
                .Include(e => e.ReceiverStudent).ThenInclude(s => s.User)
                .ToListAsync();
        }

        // GET: api/Exchanges/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Exchange>> GetExchange(int id)
        {
            var exchange = await _context.Exchanges
                .Include(e => e.InitiatorStudent).ThenInclude(s => s.User)
                .Include(e => e.ReceiverStudent).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exchange == null) return NotFound(new { message = "Intercambio no encontrado." });
            return exchange;
        }

        // GET: api/Exchanges/matches/{studentId} (Motor de Coincidencias Automáticas)
        [HttpGet("matches/{studentId}")]
        public async Task<IActionResult> GetMatchesForStudent(int studentId)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
            if (!studentExists)
            {
                return NotFound(new { message = $"El estudiante con ID {studentId} no existe en el sistema." });
            }

            var myOffers = await _context.Offers
                .Where(o => o.StudentId == studentId && o.IsActive)
                .Select(o => o.SkillId)
                .ToListAsync();

            var myRequests = await _context.Requests
                .Where(r => r.StudentId == studentId && r.IsActive)
                .Select(r => r.SkillId)
                .ToListAsync();

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

        // POST: api/Exchanges (Proponer intercambio formal)
        [HttpPost]
        public async Task<ActionResult<Exchange>> PostExchange(Exchange exchange)
        {
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

            return CreatedAtAction(nameof(GetExchange), new { id = exchange.Id }, exchange);
        }

        // PUT: api/Exchanges/5 (Actualizar estado, calificación o feedback)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExchange(int id, Exchange exchange)
        {
            if (id != exchange.Id) return BadRequest(new { message = "El ID no coincide." });

            _context.Entry(exchange).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Exchanges/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExchange(int id)
        {
            var exchange = await _context.Exchanges.FindAsync(id);
            if (exchange == null) return NotFound(new { message = "Intercambio no encontrado." });

            _context.Exchanges.Remove(exchange);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}