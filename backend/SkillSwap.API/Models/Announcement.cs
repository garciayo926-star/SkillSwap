namespace SkillSwap.API.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string DatePosted { get; set; } = string.Empty;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}