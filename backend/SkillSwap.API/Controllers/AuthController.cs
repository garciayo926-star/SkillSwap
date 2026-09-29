using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;

        public AuthController(SkillSwapDbContext context)
        {
            _context = context;
        }

        // DTO (Objeto de transferencia de datos) interno para recibir el registro
        public class RegisterDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty; // En producción recuerda encriptar con BCrypt o Hashing
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? Bio { get; set; }
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Validar si el email ya existe
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "El correo electrónico ya está registrado." });
            }

            // 1. Crear el usuario
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = dto.Password, // Nota: Asegúrate de aplicar hashing en fases avanzadas de seguridad
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 2. Crear automáticamente su perfil de Estudiante asociado
            var student = new Student
            {
                UserId = user.Id,
                Bio = dto.Bio ?? "¡Hola! Estoy listo para intercambiar habilidades en SkillSwap."
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario y perfil de estudiante registrados exitosamente.", userId = user.Id, studentId = student.Id });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.PasswordHash == dto.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Credenciales incorrectas." });
            }

            return Ok(new { 
                message = "Login exitoso", 
                userId = user.Id, 
                username = user.Username,
                studentId = user.Student?.Id 
            });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}