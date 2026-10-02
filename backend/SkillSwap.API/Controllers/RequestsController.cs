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

        // GET: api/Requests (Listar y filtrar)
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
                r.IsActive,
                StudentId = r.Student.Id,
                StudentName = r.Student.User.FirstName + " " + r.Student.User.LastName,
                SkillId = r.Skill.Id,
                SkillName = r.Skill.Name,
                oCategory = r.Skill.Category
            }).ToListAsync();

            return Ok(results);
        }

        // GET: api/Requests/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Student).ThenInclude(s => s.User)
                .Include(r => r.Skill)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound(new { message = "Solicitud no encontrada." });
            return Ok(request);
        }

        // POST: api/Requests (Crear con validaciones)
        [HttpPost]
        public async Task<ActionResult<Request>> PostRequest(Request request)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId);
            if (!studentExists) return BadRequest(new { message = "El Estudiante especificado no existe." });

            var skillExists = await _context.Skills.AnyAsync(s => s.Id == request.SkillId);
            if (!skillExists) return BadRequest(new { message = "La Habilidad especificada no existe." });

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, request);
        }

        // PUT: api/Requests/5 (Actualizar)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRequest(int id, Request request)
        {
            if (id != request.Id) return BadRequest(new { message = "El ID no coincide." });

            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Requests/5 (Eliminar)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound(new { message = "Solicitud no encontrada." });

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}