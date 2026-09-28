    namespace SkillSwap.API.Models;

    public class UserRoles
    {
        public Guid UserId { get; set; }
        public int RoleId { get; set; }
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }