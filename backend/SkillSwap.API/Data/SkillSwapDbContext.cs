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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- SEMILLA DE DATOS PARA LOS ROLES ---
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Master" },
                new Role { Id = 2, Name = "Technical" },
                new Role { Id = 3, Name = "Student" }
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
        }
    }
}