using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    // Gestión y moderación de reportes de usuarios o conflictos en intercambios.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public ReportsController(SkillSwapDbContext context)
        {
            _context = context;
        }

        public class ReportCreateDto
        {
            public Guid? ReportedUserId { get; set; }
            public int? ExchangeId { get; set; }
            public string Reason { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class ReportStatusDto
        {
            // Abierto, EnRevision, Resuelto, Descartado
            public string Status { get; set; } = string.Empty;
            public string? Resolution { get; set; }
        }

        private static readonly string[] ValidStatuses = { "Abierto", "EnRevision", "Resuelto", "Descartado" };

        // POST: api/Reports (Cualquier usuario autenticado reporta a otro usuario o un conflicto)
        [HttpPost]
        public async Task<IActionResult> CreateReport(ReportCreateDto dto)
        {
            var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(reporterId, out var reporterGuid))
                return Unauthorized(new { message = "Token inválido." });

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { message = "Debes indicar el motivo del reporte." });

            if (dto.ReportedUserId == null && dto.ExchangeId == null)
                return BadRequest(new { message = "El reporte debe apuntar a un usuario o a un intercambio." });

            if (dto.ReportedUserId.HasValue)
            {
                if (dto.ReportedUserId == reporterGuid)
                    return BadRequest(new { message = "No puedes reportarte a ti mismo." });
                if (!await _context.Users.AnyAsync(u => u.Id == dto.ReportedUserId))
                    return BadRequest(new { message = "El usuario reportado no existe." });
            }

            if (dto.ExchangeId.HasValue && !await _context.Exchanges.AnyAsync(e => e.Id == dto.ExchangeId))
                return BadRequest(new { message = "El intercambio indicado no existe." });

            var report = new Report
            {
                ReporterUserId = reporterGuid,
                ReportedUserId = dto.ReportedUserId,
                ExchangeId = dto.ExchangeId,
                Reason = dto.Reason,
                Description = dto.Description,
                Status = "Abierto",
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, new { message = "Reporte enviado. Un moderador lo revisará.", report.Id });
        }

        // GET: api/Reports?status=Abierto (Listar reportes - Moderador/Admin)
        [HttpGet]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> GetReports([FromQuery] string? status)
        {
            var query = _context.Reports.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var results = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Reason,
                    r.Description,
                    r.Status,
                    r.CreatedAt,
                    r.ResolvedAt,
                    r.Resolution,
                    Reporter = r.ReporterUser != null ? r.ReporterUser.Username : null,
                    r.ReportedUserId,
                    Reported = r.ReportedUser != null ? r.ReportedUser.Username : null,
                    r.ExchangeId
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/Reports/5 (Detalle - Moderador/Admin)
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> GetReport(int id)
        {
            var report = await _context.Reports
                .Where(r => r.Id == id)
                .Select(r => new {
                    r.Id,
                    r.Reason,
                    r.Description,
                    r.Status,
                    r.CreatedAt,
                    r.ResolvedAt,
                    r.Resolution,
                    Reporter = r.ReporterUser != null ? r.ReporterUser.Username : null,
                    r.ReportedUserId,
                    Reported = r.ReportedUser != null ? r.ReportedUser.Username : null,
                    r.ExchangeId
                })
                .FirstOrDefaultAsync();

            if (report == null) return NotFound(new { message = "Reporte no encontrado." });
            return Ok(report);
        }

        // PATCH: api/Reports/5/status (Gestionar un reporte - Moderador/Admin)
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> UpdateReportStatus(int id, ReportStatusDto dto)
        {
            if (!ValidStatuses.Contains(dto.Status))
                return BadRequest(new { message = "Estado de reporte no válido." });

            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound(new { message = "Reporte no encontrado." });

            report.Status = dto.Status;
            report.Resolution = dto.Resolution ?? report.Resolution;
            report.ResolvedAt = (dto.Status == "Resuelto" || dto.Status == "Descartado") ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Reporte actualizado a {dto.Status}.", report.Id, report.Status });
        }
    }
}
