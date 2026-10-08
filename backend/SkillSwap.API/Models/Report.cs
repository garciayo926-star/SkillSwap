namespace SkillSwap.API.Models
{
    // Reporte de moderación: un usuario reporta a otro usuario o un conflicto en un intercambio.
    public class Report
    {
        public int Id { get; set; }

        // Quién reporta
        public Guid ReporterUserId { get; set; }
        public User? ReporterUser { get; set; }

        // Usuario reportado (opcional)
        public Guid? ReportedUserId { get; set; }
        public User? ReportedUser { get; set; }

        // Intercambio en conflicto (opcional)
        public int? ExchangeId { get; set; }
        public Exchange? Exchange { get; set; }

        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Estados: Abierto, EnRevision, Resuelto, Descartado
        public string Status { get; set; } = "Abierto";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public string Resolution { get; set; } = string.Empty;
    }
}
