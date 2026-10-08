namespace SkillSwap.API.Models
{
    public class Exchange
    {
        public int Id { get; set; }
        public int InitiatorStudentId { get; set; }
        public Student? InitiatorStudent { get; set; }
        public int ReceiverStudentId { get; set; }
        public Student? ReceiverStudent { get; set; }

        // Habilidad que el iniciador enseña al receptor
        public int? OfferedSkillId { get; set; }
        public Skill? OfferedSkill { get; set; }

        // Habilidad que el iniciador quiere aprender del receptor
        public int? RequestedSkillId { get; set; }
        public Skill? RequestedSkill { get; set; }

        // Estados: Pendiente, Aceptado, Rechazado, Completado, Cancelado
        public string Status { get; set; } = "Pendiente";

        // Fechas corregidas como DateTime para coincidir con la base de datos
        public DateTime DateProposed { get; set; } = DateTime.UtcNow;
        public DateTime? DateCompleted { get; set; }

        // Fecha acordada para la sesión de intercambio (opcional)
        public DateTime? SessionDate { get; set; }

        // Calificación/feedback rápidos (resumen); el detalle vive en la entidad Review (1:1)
        public int Rating { get; set; }
        public string Feedback { get; set; } = string.Empty;

        public Review? Review { get; set; }
    }
}
