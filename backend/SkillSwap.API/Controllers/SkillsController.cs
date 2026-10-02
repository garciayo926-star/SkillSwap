using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillsController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public SkillsController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Skills (Listar con filtro opcional por categoría)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Skill>>> GetSkills([FromQuery] string? category)
        {
            var query = _context.Skills.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.Category.ToLower() == category.ToLower());
            }

            return await query.ToListAsync();
        }

        // GET: api/Skills/5 (Obtener por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Skill>> GetSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill == null) return NotFound(new { message = "Habilidad no encontrada." });
            return skill;
        }

        // POST: api/Skills (Crear - El ID es autoincremental)
        [HttpPost]
        public async Task<ActionResult<Skill>> PostSkill(Skill skill)
        {
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
        }

        // PUT: api/Skills/5 (Actualizar)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSkill(int id, Skill skill)
        {
            if (id != skill.Id) return BadRequest(new { message = "El ID no coincide con la ruta." });

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

        // DELETE: api/Skills/5 (Eliminar)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill == null) return NotFound(new { message = "Habilidad no encontrada." });

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}