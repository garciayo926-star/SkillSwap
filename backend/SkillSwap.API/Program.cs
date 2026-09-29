using SkillSwap.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SkillSwapDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var app = builder.Build();
if(app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

app.Run();
