    namespace SkillSwap.API.Models;

    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        // Flujo de aprobación del catálogo: Pendiente, Aprobada, Rechazada.
        // Lo que crea un Estudiante queda "Pendiente"; Moderador/Admin lo revisa.
        public string Status { get; set; } = "Aprobada";

        // Estudiante que la sugirió (opcional: null si la creó un Moderador/Admin)
        public int? SuggestedByStudentId { get; set; }
        public Student? SuggestedByStudent { get; set; }

        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }
