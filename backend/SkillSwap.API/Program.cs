using SkillSwap.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SkillSwapDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1. AÑADE ESTO: Configurar la política de CORS para permitir conexiones desde Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

builder.Services.AddControllers();

var app = builder.Build();

if(app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 2. AÑADE ESTO: Activar la política de CORS antes de mapear los controladores
app.UseCors("AllowAngular");

app.MapControllers();

app.Run();