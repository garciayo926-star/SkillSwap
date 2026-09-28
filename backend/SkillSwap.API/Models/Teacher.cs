namespace SkillSwap.API.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Department { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
    }
}