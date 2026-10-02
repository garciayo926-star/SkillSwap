namespace SkillSwap.API.Models
{
    public class Offer
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student? Student { get; set; } // <- El signo ? lo hace opcional

        public int SkillId { get; set; }
        public Skill? Skill { get; set; } // <- El signo ? lo hace opcional

        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}