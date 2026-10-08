using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    // El control de acceso se realiza ahora con JWT ([Authorize]) emitido en el login.
    // Las acciones sensibles (crear usuarios, roles, contraseñas) son exclusivas del Administrador.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AdminController(SkillSwapDbContext context)
        {
            _context = context;
        }

        public class RoleUpdateDto
        {
            public string RoleName { get; set; } = string.Empty;
        }

        public class StatusUpdateDto
        {
            public bool IsActive { get; set; }
        }

        public class PasswordResetDto
        {
            public string NewPassword { get; set; } = string.Empty;
        }

        public class CreateUserDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string RoleName { get; set; } = "Estudiante";
            public string? Bio { get; set; }
        }

        // GET: api/Admin/analytics (Indicadores generales para el dashboard)
        // Visible para los perfiles con visión global; el Estudiante NO accede a estas métricas.
        [HttpGet("analytics")]
        [HttpGet("dashboard")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> GetMasterAnalytics()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalStudents = await _context.Students.CountAsync();
            var totalSkills = await _context.Skills.CountAsync();
            var activeOffers = await _context.Offers.CountAsync(o => o.IsActive);
            var activeRequests = await _context.Requests.CountAsync(r => r.IsActive);

            var studentsWithActivity = await _context.Students
                .CountAsync(s => s.Offers.Any(o => o.IsActive) || s.Requests.Any(r => r.IsActive));

            var exchangesByStatus = await _context.Exchanges
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var topOfferedSkills = await _context.Skills
                .Select(s => new { s.Name, s.Category, Count = s.Offers.Count(o => o.IsActive) })
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .Take(5)
                .ToListAsync();

            var topRequestedSkills = await _context.Skills
                .Select(s => new { s.Name, s.Category, Count = s.Requests.Count(r => r.IsActive) })
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .Take(5)
                .ToListAsync();

            var skillsByCategory = await _context.Skills
                .GroupBy(s => s.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            // Los usuarios sin rol asignado se consideran Student (mismo criterio que el login)
            var usersWithoutRole = await _context.Users.CountAsync(u => !u.UserRoles.Any());
            var usersByRole = (await _context.Roles
                .OrderBy(r => r.Id)
                .Select(r => new { Role = r.Name, Count = r.UserRoles.Count })
                .ToListAsync())
                .Select(r => new { r.Role, Count = r.Role == "Estudiante" ? r.Count + usersWithoutRole : r.Count })
                .ToList();

            var averageRating = await _context.Exchanges
                .Where(e => e.Status == "Completado" && e.Rating > 0)
                .Select(e => (double?)e.Rating)
                .AverageAsync() ?? 0;

            return Ok(new {
                totalUsers,
                totalStudents,
                totalSkills,
                totalOffers = activeOffers,
                totalRequests = activeRequests,
                totalExchanges = exchangesByStatus.Sum(e => e.Count),
                studentsWithActivity,
                studentsWithoutActivity = totalStudents - studentsWithActivity,
                averageRating = Math.Round(averageRating, 1),
                exchangesByStatus,
                topOfferedSkills,
                topRequestedSkills,
                skillsByCategory,
                usersByRole,
                message = "Resumen general obtenido exitosamente."
            });
        }

        // GET: api/Admin/system-health (Estado real de la API y la base de datos)
        // El Administrador controla la configuración y el estado del sistema.
        [HttpGet("system-health")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetSystemHealth()
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var pendingMigrations = canConnect
                ? (await _context.Database.GetPendingMigrationsAsync()).ToList()
                : new List<string>();

            return Ok(new {
                apiStatus = "En línea",
                database = canConnect ? "Conectada (PostgreSQL)" : "Sin conexión",
                databaseConnected = canConnect,
                pendingMigrations,
                serverHealth = canConnect && pendingMigrations.Count == 0 ? "Saludable" : "Requiere atención",
                timestamp = DateTime.UtcNow
            });
        }

        // GET: api/Admin/users (Usuarios con su rol asignado)
        // El Administrador y el Moderador ven usuarios, correos y estado. NUNCA se expone la contraseña (está cifrada con hash).
        [HttpGet("users")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Username)
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email,
                    FullName = u.FirstName + " " + u.LastName,
                    u.IsActive,
                    u.CreatedAt,
                    StudentId = u.Student != null ? (int?)u.Student.Id : null,
                    Role = u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Estudiante"
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/Admin/users (El Administrador crea cuentas con cualquier rol)
        [HttpPost("users")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName))
            {
                return BadRequest(new { message = "Todos los campos obligatorios deben completarse." });
            }

            if (dto.Password.Length < 6)
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "El correo electrónico ya está registrado." });

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest(new { message = "El nombre de usuario ya está en uso." });

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null) return BadRequest(new { message = "El rol especificado no existe." });

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

            // El rol Estudiante tiene además un perfil de estudiante para participar en intercambios
            if (dto.RoleName == "Estudiante")
            {
                _context.Students.Add(new Student
                {
                    User = user,
                    Bio = string.IsNullOrWhiteSpace(dto.Bio)
                        ? "¡Hola! Estoy listo para intercambiar habilidades en SkillSwap."
                        : dto.Bio
                });
            }

            _context.UserRoles.Add(new UserRoles { User = user, RoleId = role.Id });
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Cuenta '{user.Username}' creada con rol {role.Name}.", userId = user.Id });
        }

        // PUT: api/Admin/users/{userId}/role (Asignar rol: Administrador, Moderador o Estudiante)
        [HttpPut("users/{userId}/role")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, RoleUpdateDto dto)
        {
            var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = "Usuario no encontrado." });

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null) return BadRequest(new { message = "El rol especificado no existe." });

            // Un usuario mantiene un único rol en el sistema
            _context.UserRoles.RemoveRange(user.UserRoles);
            _context.UserRoles.Add(new UserRoles { UserId = user.Id, RoleId = role.Id });
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Rol actualizado a {role.Name}.", role = role.Name });
        }

        // PUT: api/Admin/users/{userId}/status (Activar o suspender cuentas: Administrador y Moderador)
        [HttpPut("users/{userId}/status")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> UpdateUserStatus(Guid userId, StatusUpdateDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = "Usuario no encontrado." });

            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _context.SaveChangesAsync();

            return Ok(new { message = dto.IsActive ? "Usuario activado." : "Usuario desactivado.", isActive = user.IsActive });
        }

        // PUT: api/Admin/users/{userId}/password (El Administrador restablece la contraseña; se guarda cifrada)
        [HttpPut("users/{userId}/password")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ResetUserPassword(Guid userId, PasswordResetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = "Usuario no encontrado." });

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Contraseña de '{user.Username}' restablecida correctamente." });
        }
    }
}
