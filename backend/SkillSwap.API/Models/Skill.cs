    namespace SkillSwap.API.Models;

    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }
