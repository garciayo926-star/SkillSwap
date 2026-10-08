using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Models;

namespace SkillSwap.API.Data
{
    public class SkillSwapDbContext : DbContext
    {
        public SkillSwapDbContext(DbContextOptions<SkillSwapDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRoles> UserRoles => Set<UserRoles>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Exchange> Exchanges => Set<Exchange>();
        public DbSet<Offer> Offers => Set<Offer>();
        public DbSet<Request> Requests => Set<Request>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<Review> Reviews => Set<Review>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- SEMILLA DE DATOS PARA LOS ROLES ---
            // Roles del sistema (3): Estudiante, Moderador y Administrador
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Administrador" },
                new Role { Id = 2, Name = "Moderador" },
                new Role { Id = 3, Name = "Estudiante" }
            );

            modelBuilder.Entity<UserRoles>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<Exchange>()
                .HasOne(e => e.InitiatorStudent)
                .WithMany(s => s.ExchangesInitiated)
                .HasForeignKey(e => e.InitiatorStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exchange>()
                .HasOne(e => e.ReceiverStudent)
                .WithMany(s => s.ExchangesReceived)
                .HasForeignKey(e => e.ReceiverStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Habilidades involucradas en el intercambio (opcionales)
            modelBuilder.Entity<Exchange>()
                .HasOne(e => e.OfferedSkill)
                .WithMany()
                .HasForeignKey(e => e.OfferedSkillId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Exchange>()
                .HasOne(e => e.RequestedSkill)
                .WithMany()
                .HasForeignKey(e => e.RequestedSkillId)
                .OnDelete(DeleteBehavior.SetNull);

            // Habilidad sugerida por un estudiante (opcional); si se borra el estudiante, queda null
            modelBuilder.Entity<Skill>()
                .HasOne(s => s.SuggestedByStudent)
                .WithMany()
                .HasForeignKey(s => s.SuggestedByStudentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Reseña 1:1 con el intercambio; se borra junto con él
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Exchange)
                .WithOne(e => e.Review)
                .HasForeignKey<Review>(r => r.ExchangeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.ReviewerStudent)
                .WithMany()
                .HasForeignKey(r => r.ReviewerStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.RevieweeStudent)
                .WithMany()
                .HasForeignKey(r => r.RevieweeStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reportes de moderación: relaciones opcionales sin borrado en cascada
            modelBuilder.Entity<Report>()
                .HasOne(r => r.ReporterUser)
                .WithMany()
                .HasForeignKey(r => r.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ReportedUser)
                .WithMany()
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Exchange)
                .WithMany()
                .HasForeignKey(r => r.ExchangeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Un estudiante no puede ofrecer ni solicitar la misma habilidad dos veces
            modelBuilder.Entity<Offer>()
                .HasIndex(o => new { o.StudentId, o.SkillId })
                .IsUnique();

            modelBuilder.Entity<Request>()
                .HasIndex(r => new { r.StudentId, r.SkillId })
                .IsUnique();
        }
    }
}