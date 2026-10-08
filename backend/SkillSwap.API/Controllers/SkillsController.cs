using System.Security.Claims;
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
    public class SkillsController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public SkillsController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Skills (Listar/buscar por nombre, categoría y estado de aprobación)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSkills([FromQuery] string? category, [FromQuery] string? search, [FromQuery] string? status)
        {
            var query = _context.Skills.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.Name.ToLower().Contains(search.ToLower()));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            var results = await query
                .OrderBy(s => s.Name)
                .Select(s => new {
                    s.Id,
                    s.Name,
                    s.Category,
                    s.Status,
                    OffersCount = s.Offers.Count(o => o.IsActive),
                    RequestsCount = s.Requests.Count(r => r.IsActive)
                }).ToListAsync();

            return Ok(results);
        }

        // GET: api/Skills/categories (Categorías existentes para los filtros)
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            return await _context.Skills
                .Select(s => s.Category)
                .Where(c => c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        // GET: api/Skills/5 (Obtener por ID)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSkill(int id)
        {
            var skill = await _context.Skills
                .Where(s => s.Id == id)
                .Select(s => new {
                    s.Id,
                    s.Name,
                    s.Category,
                    s.Status,
                    OffersCount = s.Offers.Count(o => o.IsActive),
                    RequestsCount = s.Requests.Count(r => r.IsActive)
                })
                .FirstOrDefaultAsync();

            if (skill == null) return NotFound(new { message = "Habilidad no encontrada." });
            return Ok(skill);
        }

        // POST: api/Skills (Sugerir o crear una habilidad del catálogo)
        // Si la propone un Estudiante queda "Pendiente" de revisión; Moderador/Admin la crean ya "Aprobada".
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Skill>> PostSkill(Skill skill)
        {
            if (string.IsNullOrWhiteSpace(skill.Name) || string.IsNullOrWhiteSpace(skill.Category))
                return BadRequest(new { message = "El nombre y la categoría son obligatorios." });

            if (await _context.Skills.AnyAsync(s => s.Name.ToLower() == skill.Name.ToLower()))
                return BadRequest(new { message = "Ya existe una habilidad con ese nombre." });

            // El estado lo decide el servidor según el rol (no se confía en el cliente)
            var esModeradorOAdmin = User.IsInRole("Administrador") || User.IsInRole("Moderador");
            skill.Status = esModeradorOAdmin ? "Aprobada" : "Pendiente";

            // Si la sugiere un estudiante, se registra quién la propuso (sugerido_por)
            skill.SuggestedByStudentId = null;
            if (!esModeradorOAdmin && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                skill.SuggestedByStudentId = await _context.Students
                    .Where(s => s.UserId == userId)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync();
            }

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            var mensaje = esModeradorOAdmin
                ? "Habilidad creada y aprobada."
                : "Habilidad sugerida. Queda pendiente de aprobación por un moderador.";

            return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, new { skill.Id, skill.Name, skill.Category, skill.Status, message = mensaje });
        }

        public class SkillReviewDto
        {
            // "Aprobada" o "Rechazada"
            public string Status { get; set; } = string.Empty;
        }

        // PATCH: api/Skills/5/review (Aprobar o rechazar una habilidad propuesta - Moderador/Admin)
        [HttpPatch("{id}/review")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> ReviewSkill(int id, SkillReviewDto dto)
        {
            if (dto.Status != "Aprobada" && dto.Status != "Rechazada")
                return BadRequest(new { message = "El estado de revisión debe ser 'Aprobada' o 'Rechazada'." });

            var skill = await _context.Skills.FindAsync(id);
            if (skill == null) return NotFound(new { message = "Habilidad no encontrada." });

            skill.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Habilidad '{skill.Name}' marcada como {dto.Status}.", skill.Id, skill.Status });
        }

        // PUT: api/Skills/5 (Actualizar - Administrador y Moderador)
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> PutSkill(int id, Skill skill)
        {
            if (id != skill.Id) return BadRequest(new { message = "El ID no coincide con la ruta." });

            if (string.IsNullOrWhiteSpace(skill.Name) || string.IsNullOrWhiteSpace(skill.Category))
                return BadRequest(new { message = "El nombre y la categoría son obligatorios." });

            if (await _context.Skills.AnyAsync(s => s.Name.ToLower() == skill.Name.ToLower() && s.Id != id))
                return BadRequest(new { message = "Ya existe una habilidad con ese nombre." });

            _context.Entry(skill).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Skills.AnyAsync(e => e.Id == id))
                    return NotFound(new { message = "Habilidad no encontrada." });
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Skills/5 (Eliminar si no está en uso - Administrador y Moderador)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill == null) return NotFound(new { message = "Habilidad no encontrada." });

            var inUse = await _context.Offers.AnyAsync(o => o.SkillId == id)
                || await _context.Requests.AnyAsync(r => r.SkillId == id);
            if (inUse)
            {
                return BadRequest(new { message = "No se puede eliminar: la habilidad tiene ofertas o solicitudes asociadas." });
            }

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
