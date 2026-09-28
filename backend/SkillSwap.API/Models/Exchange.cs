    namespace SkillSwap.API.Models;

    public class Exchange
    {
        public int Id { get; set; }
        public int InitiatorStudentId { get; set; }
        public Student InitiatorStudent { get; set; } = null!;
        public int ReceiverStudentId { get; set; }
        public Student ReceiverStudent { get; set; } = null!;
        public string Status { get; set; } = "Pendiente";
        public string DateProposed { get; set; } = string.Empty;
        public string DateCompleted { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
