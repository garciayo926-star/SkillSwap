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

        public class RegisterDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty; 
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? Bio { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "El correo electrónico ya está registrado." });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = dto.Password, 
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                UserId = user.Id,
                Bio = dto.Bio ?? "¡Hola! Estoy listo para intercambiar habilidades en SkillSwap."
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
            
            if (studentRole != null)
            {
                var userRole = new UserRoles
                {
                    UserId = user.Id,
                    RoleId = studentRole.Id
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Usuario y perfil de estudiante registrados exitosamente.", userId = user.Id, studentId = student.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.PasswordHash == dto.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Credenciales incorrectas." });
            }

            var roleName = user.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Student";

            return Ok(new { 
                message = "Login exitoso", 
                userId = user.Id, 
                username = user.Username,
                studentId = user.Student?.Id,
                role = roleName 
            });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}