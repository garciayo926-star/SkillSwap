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

        // GET: api/Offers (Listar y filtrar)
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
                o.IsActive,
                StudentId = o.Student.Id,
                StudentName = o.Student.User.FirstName + " " + o.Student.User.LastName,
                SkillId = o.Skill.Id,
                SkillName = o.Skill.Name,
                o.Skill.Category
            }).ToListAsync();

            return Ok(results);
        }

        // GET: api/Offers/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOffer(int id)
        {
            var offer = await _context.Offers
                .Include(o => o.Student).ThenInclude(s => s.User)
                .Include(o => o.Skill)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null) return NotFound(new { message = "Oferta no encontrada." });
            return Ok(offer);
        }

        // POST: api/Offers (Crear con validación de llaves foráneas)
        [HttpPost]
        public async Task<ActionResult<Offer>> PostOffer(Offer offer)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Id == offer.StudentId);
            if (!studentExists) return BadRequest(new { message = "El Estudiante especificado no existe." });

            var skillExists = await _context.Skills.AnyAsync(s => s.Id == offer.SkillId);
            if (!skillExists) return BadRequest(new { message = "La Habilidad especificada no existe." });

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOffer), new { id = offer.Id }, offer);
        }

        // PUT: api/Offers/5 (Actualizar)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOffer(int id, Offer offer)
        {
            if (id != offer.Id) return BadRequest(new { message = "El ID no coincide." });

            _context.Entry(offer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Offers/5 (Eliminar)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOffer(int id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null) return NotFound(new { message = "Oferta no encontrada." });

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}