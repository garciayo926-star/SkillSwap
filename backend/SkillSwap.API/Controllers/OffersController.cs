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
    public class OffersController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public OffersController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Offers (Listar y filtrar por habilidad, categoría, estudiante o estado)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOffers(
            [FromQuery] string? skillName,
            [FromQuery] string? category,
            [FromQuery] int? studentId,
            [FromQuery] bool includeInactive = false)
        {
            var query = _context.Offers.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            if (studentId.HasValue)
            {
                query = query.Where(o => o.StudentId == studentId.Value);
            }

            if (!string.IsNullOrEmpty(skillName))
            {
                query = query.Where(o => o.Skill!.Name.ToLower().Contains(skillName.ToLower()));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(o => o.Skill!.Category.ToLower() == category.ToLower());
            }

            var results = await query
                .OrderByDescending(o => o.Id)
                .Select(o => new {
                    o.Id,
                    o.Description,
                    o.Level,
                    o.Modality,
                    o.IsActive,
                    StudentId = o.Student!.Id,
                    StudentName = o.Student.User!.FirstName + " " + o.Student.User.LastName,
                    SkillId = o.Skill!.Id,
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
                .Where(o => o.Id == id)
                .Select(o => new {
                    o.Id,
                    o.Description,
                    o.Level,
                    o.Modality,
                    o.IsActive,
                    StudentId = o.Student!.Id,
                    StudentName = o.Student.User!.FirstName + " " + o.Student.User.LastName,
                    SkillId = o.Skill!.Id,
                    SkillName = o.Skill.Name,
                    o.Skill.Category
                })
                .FirstOrDefaultAsync();

            if (offer == null) return NotFound(new { message = "Oferta no encontrada." });
            return Ok(offer);
        }

        // POST: api/Offers (Crear con validación de llaves foráneas)
        [HttpPost]
        public async Task<IActionResult> PostOffer(Offer offer)
        {
            var validation = await ValidateOfferAsync(offer);
            if (validation != null) return validation;

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOffer), new { id = offer.Id }, new { offer.Id, offer.StudentId, offer.SkillId, offer.Description, offer.IsActive });
        }

        // PUT: api/Offers/5 (Actualizar)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOffer(int id, Offer offer)
        {
            if (id != offer.Id) return BadRequest(new { message = "El ID no coincide." });

            if (!await _context.Offers.AnyAsync(o => o.Id == id))
                return NotFound(new { message = "Oferta no encontrada." });

            var validation = await ValidateOfferAsync(offer);
            if (validation != null) return validation;

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

        private async Task<IActionResult?> ValidateOfferAsync(Offer offer)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Id == offer.StudentId);
            if (!studentExists) return BadRequest(new { message = "El Estudiante especificado no existe." });

            var skillExists = await _context.Skills.AnyAsync(s => s.Id == offer.SkillId);
            if (!skillExists) return BadRequest(new { message = "La Habilidad especificada no existe." });

            var duplicated = await _context.Offers.AnyAsync(o => o.StudentId == offer.StudentId && o.SkillId == offer.SkillId && o.Id != offer.Id);
            if (duplicated) return BadRequest(new { message = "El estudiante ya ofrece esta habilidad." });

            var alreadyRequests = await _context.Requests.AnyAsync(r => r.StudentId == offer.StudentId && r.SkillId == offer.SkillId);
            if (alreadyRequests) return BadRequest(new { message = "No puedes ofrecer una habilidad que ya solicitas." });

            return null;
        }
    }
}
