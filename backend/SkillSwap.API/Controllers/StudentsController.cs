using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Data;
using SkillSwap.API.Models;

namespace SkillSwap.API.Controllers
{
    // Requiere sesión (token). La administración se restringe por método;
    // un estudiante solo puede ver SU propio perfil.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly SkillSwapDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public StudentsController(SkillSwapDbContext context)
        {
            _context = context;
        }

        public class StudentCreateDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? Bio { get; set; }
        }

        public class StudentUpdateDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Bio { get; set; } = string.Empty;
            public bool IsActive { get; set; } = true;
        }

        // GET: api/Students (Listar y filtrar por nombre o por habilidad ofrecida/solicitada)
        [HttpGet]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<ActionResult<IEnumerable<object>>> GetStudents([FromQuery] string? name, [FromQuery] string? skillName)
        {
            var query = _context.Students.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                var term = name.ToLower();
                query = query.Where(s => s.User!.FirstName.ToLower().Contains(term)
                    || s.User.LastName.ToLower().Contains(term)
                    || s.User.Username.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(skillName))
            {
                var term = skillName.ToLower();
                query = query.Where(s => s.Offers.Any(o => o.Skill!.Name.ToLower().Contains(term))
                    || s.Requests.Any(r => r.Skill!.Name.ToLower().Contains(term)));
            }

            var results = await query
                .OrderBy(s => s.User!.FirstName)
                .Select(s => new {
                    s.Id,
                    s.UserId,
                    s.User!.Username,
                    FullName = s.User.FirstName + " " + s.User.LastName,
                    s.User.FirstName,
                    s.User.LastName,
                    s.User.Email,
                    s.Bio,
                    s.Reputation,
                    s.User.IsActive,
                    OfferedSkills = s.Offers.Where(o => o.IsActive).Select(o => o.Skill!.Name).ToList(),
                    RequestedSkills = s.Requests.Where(r => r.IsActive).Select(r => r.Skill!.Name).ToList(),
                    ExchangesCount = s.ExchangesInitiated.Count + s.ExchangesReceived.Count
                }).ToListAsync();

            return Ok(results);
        }

        // GET: api/Students/5 (Perfil con sus ofertas, solicitudes y resumen de intercambios)
        // Accesible a Administrador/Moderador, y a un estudiante SOLO para su propio perfil.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students
                .Where(s => s.Id == id)
                .Select(s => new {
                    s.Id,
                    s.UserId,
                    s.User!.Username,
                    FullName = s.User.FirstName + " " + s.User.LastName,
                    s.User.FirstName,
                    s.User.LastName,
                    s.User.Email,
                    s.Bio,
                    s.Reputation,
                    s.User.IsActive,
                    Offers = s.Offers.Select(o => new { o.Id, o.SkillId, SkillName = o.Skill!.Name, o.Skill.Category, o.Level, o.Modality, o.Description, o.IsActive }).ToList(),
                    Requests = s.Requests.Select(r => new { r.Id, r.SkillId, SkillName = r.Skill!.Name, r.Skill.Category, r.DesiredLevel, r.Notes, r.IsActive }).ToList(),
                    ExchangesPending = s.ExchangesInitiated.Count(e => e.Status == "Pendiente") + s.ExchangesReceived.Count(e => e.Status == "Pendiente"),
                    ExchangesActive = s.ExchangesInitiated.Count(e => e.Status == "Aceptado") + s.ExchangesReceived.Count(e => e.Status == "Aceptado"),
                    ExchangesCompleted = s.ExchangesInitiated.Count(e => e.Status == "Completado") + s.ExchangesReceived.Count(e => e.Status == "Completado")
                })
                .FirstOrDefaultAsync();

            if (student == null) return NotFound(new { message = "Estudiante no encontrado." });

            // Un estudiante solo puede ver su propio perfil; Administrador/Moderador ven cualquiera.
            var esAdminOModerador = User.IsInRole("Administrador") || User.IsInRole("Moderador");
            if (!esAdminOModerador)
            {
                Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
                if (student.UserId != userId)
                    return Forbid();
            }

            return Ok(student);
        }

        // POST: api/Students (Crear usuario + perfil de estudiante. Solo el Administrador crea cuentas)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PostStudent(StudentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName))
            {
                return BadRequest(new { message = "Todos los campos obligatorios deben completarse." });
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "El correo electrónico ya está registrado." });

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest(new { message = "El nombre de usuario ya está en uso." });

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

            var student = new Student
            {
                User = user,
                Bio = string.IsNullOrWhiteSpace(dto.Bio)
                    ? "¡Hola! Estoy listo para intercambiar habilidades en SkillSwap."
                    : dto.Bio
            };

            _context.Students.Add(student);

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Estudiante");
            if (studentRole != null)
            {
                _context.UserRoles.Add(new UserRoles { User = user, RoleId = studentRole.Id });
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, new { message = "Estudiante creado exitosamente.", studentId = student.Id, userId = user.Id });
        }

        // PUT: api/Students/5 (Actualizar datos del usuario y la biografía)
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> PutStudent(int id, StudentUpdateDto dto)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (student == null || student.User == null) return NotFound(new { message = "Estudiante no encontrado." });

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != student.UserId))
                return BadRequest(new { message = "El correo electrónico ya está registrado por otro usuario." });

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != student.UserId))
                return BadRequest(new { message = "El nombre de usuario ya está en uso." });

            student.Bio = dto.Bio;
            student.User.Username = dto.Username;
            student.User.Email = dto.Email;
            student.User.FirstName = dto.FirstName;
            student.User.LastName = dto.LastName;
            student.User.IsActive = dto.IsActive;
            student.User.UpdatedAt = DateTime.UtcNow.ToString("o");

            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class StatusDto
        {
            public bool IsActive { get; set; }
        }

        // PATCH: api/Students/5/status (Activar o suspender la cuenta - Administrador y Moderador)
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrador,Moderador")]
        public async Task<IActionResult> UpdateStatus(int id, StatusDto dto)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (student == null || student.User == null) return NotFound(new { message = "Estudiante no encontrado." });

            student.User.IsActive = dto.IsActive;
            student.User.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _context.SaveChangesAsync();

            return Ok(new { message = dto.IsActive ? "Cuenta activada." : "Cuenta suspendida.", isActive = student.User.IsActive });
        }

        // DELETE: api/Students/5 (Elimina el estudiante, sus ofertas/solicitudes y su usuario - solo Administrador)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return NotFound(new { message = "Estudiante no encontrado." });

            var hasExchanges = await _context.Exchanges.AnyAsync(e => e.InitiatorStudentId == id || e.ReceiverStudentId == id);
            if (hasExchanges)
            {
                return BadRequest(new { message = "No se puede eliminar: el estudiante tiene intercambios registrados. Puedes desactivarlo en su lugar." });
            }

            // Ofertas y solicitudes se eliminan en cascada junto con el estudiante
            _context.Students.Remove(student);
            if (student.User != null)
            {
                _context.Users.Remove(student.User);
            }
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
