using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;

namespace SkillSwap.API.Controllers
{
    // Motor de coincidencia: cruza lo que un estudiante OFRECE con lo que otros SOLICITAN y viceversa.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchingController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public MatchingController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // GET: api/Matching/suggestions?studentId=1&includePartial=false
        // Match directo: el otro ofrece lo que yo busco Y busca lo que yo ofrezco (coincidencia mutua).
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] int studentId, [FromQuery] bool includePartial = false)
        {
            return await BuildMatches(studentId, includePartial, category: null, matchType: null);
        }

        // GET: api/Matching/filter?studentId=1&category=Diseño&matchType=Mutua&includePartial=true
        // Búsqueda avanzada: filtra las coincidencias por categoría de la habilidad o por tipo de match.
        [HttpGet("filter")]
        public async Task<IActionResult> Filter(
            [FromQuery] int studentId,
            [FromQuery] string? category,
            [FromQuery] string? matchType,
            [FromQuery] bool includePartial = true)
        {
            return await BuildMatches(studentId, includePartial, category, matchType);
        }

        private async Task<IActionResult> BuildMatches(int studentId, bool includePartial, string? category, string? matchType)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
            if (!studentExists)
                return NotFound(new { message = $"El estudiante con ID {studentId} no existe en el sistema." });

            var myOffers = await _context.Offers
                .Where(o => o.StudentId == studentId && o.IsActive)
                .Select(o => o.SkillId)
                .ToListAsync();

            var myRequests = await _context.Requests
                .Where(r => r.StudentId == studentId && r.IsActive)
                .Select(r => r.SkillId)
                .ToListAsync();

            var candidates = await _context.Students
                .Where(s => s.Id != studentId && s.User!.IsActive)
                .Where(s => s.Offers.Any(o => o.IsActive && myRequests.Contains(o.SkillId))
                         || s.Requests.Any(r => r.IsActive && myOffers.Contains(r.SkillId)))
                .Select(other => new {
                    StudentId = other.Id,
                    FullName = other.User!.FirstName + " " + other.User.LastName,
                    other.Bio,
                    OfferedSkillsToMe = other.Offers
                        .Where(o => myRequests.Contains(o.SkillId) && o.IsActive)
                        .Select(o => new { o.SkillId, SkillName = o.Skill!.Name, o.Skill.Category }).ToList(),
                    WantedSkillsFromMe = other.Requests
                        .Where(r => myOffers.Contains(r.SkillId) && r.IsActive)
                        .Select(r => new { r.SkillId, SkillName = r.Skill!.Name, r.Skill.Category }).ToList()
                })
                .ToListAsync();

            var matches = candidates
                .Select(m => new {
                    m.StudentId,
                    m.FullName,
                    m.Bio,
                    m.OfferedSkillsToMe,
                    m.WantedSkillsFromMe,
                    MatchType = m.OfferedSkillsToMe.Any() && m.WantedSkillsFromMe.Any() ? "Mutua" : "Parcial"
                })
                .Where(m => includePartial || m.MatchType == "Mutua")
                .Where(m => string.IsNullOrEmpty(matchType) || m.MatchType == matchType)
                .Where(m => string.IsNullOrEmpty(category)
                    || m.OfferedSkillsToMe.Any(s => s.Category.ToLower() == category.ToLower())
                    || m.WantedSkillsFromMe.Any(s => s.Category.ToLower() == category.ToLower()))
                .OrderBy(m => m.MatchType == "Mutua" ? 0 : 1)
                .ThenByDescending(m => m.OfferedSkillsToMe.Count + m.WantedSkillsFromMe.Count)
                .ToList();

            return Ok(matches);
        }
    }
}
