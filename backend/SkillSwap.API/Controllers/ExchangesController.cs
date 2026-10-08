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
    public class ExchangesController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        // Transiciones de estado permitidas en el ciclo de vida de un intercambio
        private static readonly Dictionary<string, string[]> AllowedTransitions = new()
        {
            ["Pendiente"] = new[] { "Aceptado", "Rechazado", "Cancelado" },
            ["Aceptado"] = new[] { "Completado", "Cancelado" },
            ["Rechazado"] = Array.Empty<string>(),
            ["Completado"] = Array.Empty<string>(),
            ["Cancelado"] = Array.Empty<string>()
        };

        public ExchangesController(SkillSwapDbContext context)
        {
            _context = context;
        }

        public class ExchangeStatusDto
        {
            public string Status { get; set; } = string.Empty;
            public int? Rating { get; set; }
            public string? Feedback { get; set; }
        }

        // GET: api/Exchanges (Listar con filtros por estudiante y estado)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetExchanges([FromQuery] int? studentId, [FromQuery] string? status)
        {
            var query = _context.Exchanges.AsQueryable();

            if (studentId.HasValue)
            {
                query = query.Where(e => e.InitiatorStudentId == studentId.Value || e.ReceiverStudentId == studentId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            var results = await query
                .OrderByDescending(e => e.DateProposed)
                .Select(e => new {
                    e.Id,
                    e.InitiatorStudentId,
                    InitiatorName = e.InitiatorStudent!.User!.FirstName + " " + e.InitiatorStudent.User.LastName,
                    e.ReceiverStudentId,
                    ReceiverName = e.ReceiverStudent!.User!.FirstName + " " + e.ReceiverStudent.User.LastName,
                    e.OfferedSkillId,
                    OfferedSkillName = e.OfferedSkill != null ? e.OfferedSkill.Name : null,
                    e.RequestedSkillId,
                    RequestedSkillName = e.RequestedSkill != null ? e.RequestedSkill.Name : null,
                    e.Status,
                    e.DateProposed,
                    e.DateCompleted,
                    e.Rating,
                    e.Feedback
                }).ToListAsync();

            return Ok(results);
        }

        // GET: api/Exchanges/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExchange(int id)
        {
            var exchange = await _context.Exchanges
                .Where(e => e.Id == id)
                .Select(e => new {
                    e.Id,
                    e.InitiatorStudentId,
                    InitiatorName = e.InitiatorStudent!.User!.FirstName + " " + e.InitiatorStudent.User.LastName,
                    e.ReceiverStudentId,
                    ReceiverName = e.ReceiverStudent!.User!.FirstName + " " + e.ReceiverStudent.User.LastName,
                    e.OfferedSkillId,
                    OfferedSkillName = e.OfferedSkill != null ? e.OfferedSkill.Name : null,
                    e.RequestedSkillId,
                    RequestedSkillName = e.RequestedSkill != null ? e.RequestedSkill.Name : null,
                    e.Status,
                    e.DateProposed,
                    e.DateCompleted,
                    e.Rating,
                    e.Feedback
                })
                .FirstOrDefaultAsync();

            if (exchange == null) return NotFound(new { message = "Intercambio no encontrado." });
            return Ok(exchange);
        }

        // GET: api/Exchanges/matches/{studentId} (Motor de Coincidencias Automáticas)
        // Coincidencia mutua: el otro ofrece lo que yo busco Y busca lo que yo ofrezco.
        // Con includePartial=true también se devuelven coincidencias en una sola dirección.
        [HttpGet("matches/{studentId}")]
        public async Task<IActionResult> GetMatchesForStudent(int studentId, [FromQuery] bool includePartial = false)
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
            if (!studentExists)
            {
                return NotFound(new { message = $"El estudiante con ID {studentId} no existe en el sistema." });
            }

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
                .Select(otherStudent => new {
                    StudentId = otherStudent.Id,
                    FullName = otherStudent.User!.FirstName + " " + otherStudent.User.LastName,
                    otherStudent.Bio,
                    OfferedSkillsToMe = otherStudent.Offers
                        .Where(o => myRequests.Contains(o.SkillId) && o.IsActive)
                        .Select(o => new { o.SkillId, SkillName = o.Skill!.Name }).ToList(),
                    WantedSkillsFromMe = otherStudent.Requests
                        .Where(r => myOffers.Contains(r.SkillId) && r.IsActive)
                        .Select(r => new { r.SkillId, SkillName = r.Skill!.Name }).ToList()
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
                .OrderBy(m => m.MatchType == "Mutua" ? 0 : 1)
                .ThenByDescending(m => m.OfferedSkillsToMe.Count + m.WantedSkillsFromMe.Count)
                .ToList();

            return Ok(matches);
        }

        // POST: api/Exchanges (Proponer intercambio formal)
        [HttpPost]
        public async Task<IActionResult> PostExchange(Exchange exchange)
        {
            var initiatorExists = await _context.Students.AnyAsync(s => s.Id == exchange.InitiatorStudentId);
            var receiverExists = await _context.Students.AnyAsync(s => s.Id == exchange.ReceiverStudentId);

            if (!initiatorExists || !receiverExists)
            {
                return BadRequest(new { message = "Uno de los IDs de los estudiantes participantes no es válido o no existe." });
            }

            if (exchange.InitiatorStudentId == exchange.ReceiverStudentId)
            {
                return BadRequest(new { message = "Un estudiante no puede realizar un intercambio consigo mismo." });
            }

            // La habilidad que se enseña debe estar ofrecida por el iniciador
            if (exchange.OfferedSkillId.HasValue &&
                !await _context.Offers.AnyAsync(o => o.StudentId == exchange.InitiatorStudentId && o.SkillId == exchange.OfferedSkillId && o.IsActive))
            {
                return BadRequest(new { message = "La habilidad ofrecida no está registrada como oferta activa del iniciador." });
            }

            // La habilidad que se quiere aprender debe estar ofrecida por el receptor
            if (exchange.RequestedSkillId.HasValue &&
                !await _context.Offers.AnyAsync(o => o.StudentId == exchange.ReceiverStudentId && o.SkillId == exchange.RequestedSkillId && o.IsActive))
            {
                return BadRequest(new { message = "La habilidad solicitada no está registrada como oferta activa del receptor." });
            }

            var duplicated = await _context.Exchanges.AnyAsync(e =>
                e.InitiatorStudentId == exchange.InitiatorStudentId &&
                e.ReceiverStudentId == exchange.ReceiverStudentId &&
                e.OfferedSkillId == exchange.OfferedSkillId &&
                e.RequestedSkillId == exchange.RequestedSkillId &&
                (e.Status == "Pendiente" || e.Status == "Aceptado"));
            if (duplicated)
            {
                return BadRequest(new { message = "Ya existe un intercambio en curso con estos mismos datos." });
            }

            exchange.Status = "Pendiente";
            exchange.DateProposed = DateTime.UtcNow;
            exchange.DateCompleted = null;
            exchange.Rating = 0;
            exchange.Feedback = string.Empty;

            _context.Exchanges.Add(exchange);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExchange), new { id = exchange.Id }, new { exchange.Id, exchange.Status, exchange.DateProposed });
        }

        // PUT: api/Exchanges/5/status (Aceptar, rechazar, cancelar o completar con calificación)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, ExchangeStatusDto dto)
        {
            var exchange = await _context.Exchanges.FindAsync(id);
            if (exchange == null) return NotFound(new { message = "Intercambio no encontrado." });

            if (!AllowedTransitions.TryGetValue(exchange.Status, out var allowed) || !allowed.Contains(dto.Status))
            {
                return BadRequest(new { message = $"No se puede cambiar el estado de '{exchange.Status}' a '{dto.Status}'." });
            }

            if (dto.Status == "Completado")
            {
                if (dto.Rating is < 1 or > 5)
                    return BadRequest(new { message = "La calificación debe estar entre 1 y 5." });

                exchange.Rating = dto.Rating ?? 0;
                exchange.Feedback = dto.Feedback ?? string.Empty;
                exchange.DateCompleted = DateTime.UtcNow;
            }

            exchange.Status = dto.Status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Exchanges/5/status (Aceptar, rechazar o cancelar una propuesta - alias REST del PUT)
        [HttpPatch("{id}/status")]
        public Task<IActionResult> PatchStatus(int id, ExchangeStatusDto dto) => UpdateStatus(id, dto);

        public class ReviewDto
        {
            public int Rating { get; set; }
            public string? Feedback { get; set; }
        }

        // POST: api/Exchanges/5/review (Marcar como completado y dejar calificación/reseña del compañero)
        // Crea la entidad Review (1:1), actualiza el resumen del intercambio y recalcula la reputación del calificado.
        [HttpPost("{id}/review")]
        public async Task<IActionResult> ReviewExchange(int id, ReviewDto dto)
        {
            var exchange = await _context.Exchanges
                .Include(e => e.InitiatorStudent)
                .Include(e => e.ReceiverStudent)
                .Include(e => e.Review)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (exchange == null) return NotFound(new { message = "Intercambio no encontrado." });

            // Solo se puede reseñar un intercambio aceptado (en curso) o ya completado
            if (exchange.Status != "Aceptado" && exchange.Status != "Completado")
                return BadRequest(new { message = $"No se puede reseñar un intercambio en estado '{exchange.Status}'. Debe estar aceptado." });

            if (dto.Rating is < 1 or > 5)
                return BadRequest(new { message = "La calificación debe estar entre 1 y 5." });

            // Determina quién califica y quién es calificado a partir del token
            int reviewerStudentId = exchange.InitiatorStudentId;
            int revieweeStudentId = exchange.ReceiverStudentId;
            if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                if (exchange.ReceiverStudent?.UserId == userId)
                {
                    reviewerStudentId = exchange.ReceiverStudentId;
                    revieweeStudentId = exchange.InitiatorStudentId;
                }
            }

            // Reseña 1:1: se crea o se actualiza
            if (exchange.Review == null)
            {
                exchange.Review = new Review
                {
                    ExchangeId = exchange.Id,
                    ReviewerStudentId = reviewerStudentId,
                    RevieweeStudentId = revieweeStudentId,
                    Score = dto.Rating,
                    Comment = dto.Feedback ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Reviews.Add(exchange.Review);
            }
            else
            {
                exchange.Review.ReviewerStudentId = reviewerStudentId;
                exchange.Review.RevieweeStudentId = revieweeStudentId;
                exchange.Review.Score = dto.Rating;
                exchange.Review.Comment = dto.Feedback ?? string.Empty;
                exchange.Review.CreatedAt = DateTime.UtcNow;
            }

            // Resumen rápido en el intercambio (compatibilidad) y cierre del ciclo
            exchange.Rating = dto.Rating;
            exchange.Feedback = dto.Feedback ?? string.Empty;
            exchange.Status = "Completado";
            exchange.DateCompleted ??= DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Recalcula la reputación del calificado = promedio de las reseñas recibidas
            var reviewee = await _context.Students.FirstOrDefaultAsync(s => s.Id == revieweeStudentId);
            if (reviewee != null)
            {
                reviewee.Reputation = await _context.Reviews
                    .Where(r => r.RevieweeStudentId == revieweeStudentId)
                    .AverageAsync(r => (double)r.Score);
                await _context.SaveChangesAsync();
            }

            return Ok(new {
                message = "Intercambio completado y reseña registrada.",
                exchange.Id,
                score = dto.Rating,
                revieweeStudentId,
                newReputation = reviewee != null ? Math.Round(reviewee.Reputation, 2) : 0
            });
        }

        // PUT: api/Exchanges/5 (Actualizar estado, calificación o feedback)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExchange(int id, Exchange exchange)
        {
            if (id != exchange.Id) return BadRequest(new { message = "El ID no coincide." });

            if (!await _context.Exchanges.AnyAsync(e => e.Id == id))
                return NotFound(new { message = "Intercambio no encontrado." });

            if (!AllowedTransitions.ContainsKey(exchange.Status))
                return BadRequest(new { message = "Estado de intercambio no válido." });

            if (exchange.Rating is < 0 or > 5)
                return BadRequest(new { message = "La calificación debe estar entre 1 y 5." });

            _context.Entry(exchange).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Exchanges/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExchange(int id)
        {
            var exchange = await _context.Exchanges.FindAsync(id);
            if (exchange == null) return NotFound(new { message = "Intercambio no encontrado." });

            _context.Exchanges.Remove(exchange);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
