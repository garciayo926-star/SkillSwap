namespace SkillSwap.API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
        public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    }
}