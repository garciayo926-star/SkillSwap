namespace SkillSwap.API.Models
{
    public class Request
    {
        public int Id { get; set; }
        
        public int StudentId { get; set; }
        public Student? Student { get; set; } // <- Añadir el ? aquí
        
        public int SkillId { get; set; }
        public Skill? Skill { get; set; } // <- Añadir el ? aquí

        // Nivel que desea alcanzar: Básico, Intermedio, Avanzado
        public string DesiredLevel { get; set; } = "Básico";

        // Comentarios / expectativas del aprendizaje
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}