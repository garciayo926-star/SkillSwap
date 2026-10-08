using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RequestsController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public RequestsController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Requests (Listar y filtrar por habilidad, categoría, estudiante o estado)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetRequests(
            [FromQuery] string? skillName,
            [FromQuery] string? category,
            [FromQuery] int? studentId,
            [FromQuery] bool includeInactive = false)
        {
            var query = _context.Requests.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(r => r.IsActive);
            }

            if (studentId.HasValue)
            {
                query = query.Where(r => r.StudentId == studentId.Value);
            }

            if (!string.IsNullOrEmpty(skillName))
            {
                query = query.Where(r => r.Skill!.Name.ToLower().Contains(skillName.ToLower()));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(r => r.Skill!.Category.ToLower() == category.ToLower());
            }

            var results = await query
                .OrderByDescending(r => r.Id)
                .Select(r => new {
                    r.Id,
                    r.Notes,
                    r.DesiredLevel,
                    r.IsActive,
                    StudentId = r.Student!.Id,
                    StudentName = r.Student.User!.FirstName + " " + r.Student.User.LastName,
                    SkillId = r.Skill!.Id,
                    SkillName = r.Skill.Name,
                    r.Skill.Category
                }).ToListAsync();

            return Ok(results);
        }

        // GET: api/Requests/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var request = await _context.Requests
                .Where(r => r.Id == id)
                .Select(r => new {
                    r.Id,
                    r.Notes,
                    r.DesiredLevel,
                    r.IsActive,
                    StudentId = r.Student!.Id,
                    StudentName = r.Student.User!.FirstName + " " + r.Student.User.LastName,
                    SkillId = r.Skill!.Id,
                    SkillName = r.Skill.Name,
                    r.Skill.Category
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound(new { message = "Solicitud no encontrada." });
            return Ok(request);
        }

        // POST: api/Requests (Crear con validaciones)
        [HttpPost]
        public async Task<IActionResult> PostRequest(Request request)
        {
            var validation = await ValidateRequestAsync(request);
            if (validation != null) return validation;

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, new { request.Id, request.StudentId, request.SkillId, request.Notes, request.IsActive });
        }

        // PUT: api/Requests/5 (Actualizar)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRequest(int id, Request request)
        {
            if (id != request.Id) return BadRequest(new { message = "El ID no coincide." });

            if (!await _context.Requests.AnyAsync(r => r.Id == id))
                return NotFound(new { message = "Solicitud no encontrada." });

            var validation = await ValidateRequestAsync(request);
            if (validation != null) return validation;

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

        private async Task<IActionResult?> ValidateRequestAsync(Request request)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId);
            if (!studentExists) return BadRequest(new { message = "El Estudiante especificado no existe." });

            var skillExists = await _context.Skills.AnyAsync(s => s.Id == request.SkillId);
            if (!skillExists) return BadRequest(new { message = "La Habilidad especificada no existe." });

            var duplicated = await _context.Requests.AnyAsync(r => r.StudentId == request.StudentId && r.SkillId == request.SkillId && r.Id != request.Id);
            if (duplicated) return BadRequest(new { message = "El estudiante ya solicitó esta habilidad." });

            var alreadyOffers = await _context.Offers.AnyAsync(o => o.StudentId == request.StudentId && o.SkillId == request.SkillId);
            if (alreadyOffers) return BadRequest(new { message = "No puedes solicitar una habilidad que ya ofreces." });

            return null;
        }
    }
}
