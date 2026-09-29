using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public RequestsController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Requests (Filtros por Habilidad o Categoría)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetRequests([FromQuery] string? skillName, [FromQuery] string? category)
        {
            var query = _context.Requests
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Include(r => r.Skill)
                .Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(skillName))
            {
                query = query.Where(r => r.Skill.Name.ToLower().Contains(skillName.ToLower()));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(r => r.Skill.Category.ToLower() == category.ToLower());
            }

            var results = await query.Select(r => new {
                r.Id,
                r.Notes,
                StudentId = r.Student.Id,
                StudentName = r.Student.User.FirstName + " " + r.Student.User.LastName,
                StudentEmail = r.Student.User.Email,
                SkillId = r.Skill.Id,
                SkillName = r.Skill.Name,
                r.Skill.Category
            }).ToListAsync();

            return Ok(results);
        }

        // POST: api/Requests (Con validación de existencia de IDs)
        [HttpPost]
        public async Task<ActionResult<Request>> PostRequest(Request request)
        {
            // VALIDACIÓN DE ID: Verificar que el estudiante exista
            var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId);
            if (!studentExists)
            {
                return BadRequest(new { message = $"El Estudiante con ID {request.StudentId} no existe." });
            }

            // VALIDACIÓN DE ID: Verificar que la habilidad exista
            var skillExists = await _context.Skills.AnyAsync(s => s.Id == request.SkillId);
            if (!skillExists)
            {
                return BadRequest(new { message = $"La Habilidad con ID {request.SkillId} no existe en el catálogo." });
            }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRequests), new { id = request.Id }, request);
        }
    }
}