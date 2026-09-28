namespace SkillSwap.API.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
    }
}