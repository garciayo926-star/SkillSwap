    namespace SkillSwap.API.Models;

    public class Student
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Bio { get; set; } = string.Empty;

        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
        public ICollection<Request> Requests { get; set; } = new List<Request>();
        public ICollection<Exchange> ExchangesInitiated { get; set; } = new List<Exchange>();
        public ICollection<Exchange> ExchangesReceived { get; set; } = new List<Exchange>();
    }
