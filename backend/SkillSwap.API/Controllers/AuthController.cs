using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;
using SkillSwap.API.Services;

namespace SkillSwap.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;
        private readonly TokenService _tokenService;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(SkillSwapDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
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
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName))
            {
                return BadRequest(new { message = "Todos los campos obligatorios deben completarse." });
            }

            if (dto.Password.Length < 6)
            {
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "El correo electrónico ya está registrado." });
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return BadRequest(new { message = "El nombre de usuario ya está en uso." });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                UserId = user.Id,
                Bio = string.IsNullOrWhiteSpace(dto.Bio)
                    ? "¡Hola! Estoy listo para intercambiar habilidades en SkillSwap."
                    : dto.Bio
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Estudiante");

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
            // Validación de entrada: campos vacíos
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Ingresa tu correo electrónico y tu contraseña." });
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            // Mensaje genérico a propósito: no se revela si el fallo fue el correo o la contraseña
            // (buena práctica de seguridad para no filtrar qué correos existen).
            if (user == null || !await VerifyPasswordAsync(user, dto.Password))
            {
                return Unauthorized(new { message = "Correo o contraseña incorrectos." });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Tu cuenta está desactivada. Contacta al administrador." });
            }

            var roleName = user.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Estudiante";
            var (token, expiresAt) = _tokenService.CreateToken(user, roleName);

            return Ok(new {
                message = "Login exitoso",
                token,
                expiresAt,
                userId = user.Id,
                username = user.Username,
                studentId = user.Student?.Id,
                role = roleName
            });
        }

        // GET: api/Auth/me (Datos del usuario autenticado actualmente, leídos desde el token JWT)
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token inválido." });

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound(new { message = "Usuario no encontrado." });

            return Ok(new {
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                fullName = user.FirstName + " " + user.LastName,
                isActive = user.IsActive,
                studentId = user.Student?.Id,
                role = user.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Estudiante"
            });
        }

        // Verifica el hash; las cuentas antiguas guardadas en texto plano se migran a hash en el primer login
        private async Task<bool> VerifyPasswordAsync(User user, string password)
        {
            try
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                if (result == PasswordVerificationResult.Success) return true;
                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, password);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (FormatException)
            {
                // El valor almacenado no es un hash válido (contraseña antigua en texto plano)
            }

            if (user.PasswordHash == password)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                user.UpdatedAt = DateTime.UtcNow.ToString("o");
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
