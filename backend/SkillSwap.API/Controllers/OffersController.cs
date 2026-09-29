using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public OffersController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Offers (Filtros avanzados por Habilidad o Categoría)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOffers([FromQuery] string? skillName, [FromQuery] string? category)
        {
            var query = _context.Offers
                .Include(o => o.Student)
                .ThenInclude(s => s.User)
                .Include(o => o.Skill)
                .Where(o => o.IsActive);

            if (!string.IsNullOrEmpty(skillName))
            {
                query = query.Where(o => o.Skill.Name.ToLower().Contains(skillName.ToLower()));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(o => o.Skill.Category.ToLower() == category.ToLower());
            }

            var results = await query.Select(o => new {
                o.Id,
                o.Description,
                StudentId = o.Student.Id,
                StudentName = o.Student.User.FirstName + " " + o.Student.User.LastName,
                StudentEmail = o.Student.User.Email,
                SkillId = o.Skill.Id,
                SkillName = o.Skill.Name,
                o.Skill.Category
            }).ToListAsync();

            return Ok(results);
        }

        // POST: api/Offers (Con validación de existencia de IDs)
        [HttpPost]
        public async Task<ActionResult<Offer>> PostOffer(Offer offer)
        {
            // VALIDACIÓN DE ID: Verificar que el estudiante exista
            var studentExists = await _context.Students.AnyAsync(s => s.Id == offer.StudentId);
            if (!studentExists)
            {
                return BadRequest(new { message = $"El Estudiante con ID {offer.StudentId} no existe." });
            }

            // VALIDACIÓN DE ID: Verificar que la habilidad exista
            var skillExists = await _context.Skills.AnyAsync(s => s.Id == offer.SkillId);
            if (!skillExists)
            {
                return BadRequest(new { message = $"La Habilidad con ID {offer.SkillId} no existe en el catálogo." });
            }

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOffers), new { id = offer.Id }, offer);
        }
    }
}