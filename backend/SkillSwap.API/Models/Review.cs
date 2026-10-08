namespace SkillSwap.API.Models
{
    // Reseña: califica la experiencia de un intercambio (relación 1:1 con Exchange).
    public class Review
    {
        public int Id { get; set; }

        // Intercambio reseñado (1:1)
        public int ExchangeId { get; set; }
        public Exchange? Exchange { get; set; }

        // Estudiante que emite la reseña
        public int ReviewerStudentId { get; set; }
        public Student? ReviewerStudent { get; set; }

        // Estudiante que recibe la reseña (sube/baja su reputación)
        public int RevieweeStudentId { get; set; }
        public Student? RevieweeStudent { get; set; }

        public int Score { get; set; }        // 1 a 5
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
