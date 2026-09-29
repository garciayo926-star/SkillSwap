namespace SkillSwap.API.Models
{
    public class Exchange
    {
        public int Id { get; set; }
        public int InitiatorStudentId { get; set; }
        public Student InitiatorStudent { get; set; } = null!;
        public int ReceiverStudentId { get; set; }
        public Student ReceiverStudent { get; set; } = null!;
        public string Status { get; set; } = "Pendiente";
        
        // Fechas corregidas como DateTime para coincidir con la base de datos
        public DateTime DateProposed { get; set; } = DateTime.UtcNow;
        public DateTime? DateCompleted { get; set; }
        
        public int Rating { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
}