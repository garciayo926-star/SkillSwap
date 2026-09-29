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

        // GET: api/Skills (Listar todas con opción de filtrar por categoría)
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

        // POST: api/Skills (Crear una nueva habilidad)
        [HttpPost]
        public async Task<ActionResult<Skill>> PostSkill(Skill skill)
        {
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSkills), new { id = skill.Id }, skill);
        }
    }
}