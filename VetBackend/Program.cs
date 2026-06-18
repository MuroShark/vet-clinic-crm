using Microsoft.EntityFrameworkCore;
using VetClinicBackend.Data;
using VetClinicBackend.Auth;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TokenService>();

// JWT-аутентификация
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSvelteApp", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Предотвращает зависание сериализации при циклических навигационных свойствах
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Имена свойств передаются как объявлены в DTO (напр. KPIRate), без camelCase-преобразования
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Создать схему БД и заполнить справочными данными при первом запуске.
// Повторные попытки нужны при Docker-запуске: SQL Server стартует дольше бэкенда.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (int attempt = 1; attempt <= 5; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            await DbInitializer.SeedAsync(db);
            break;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Startup] БД не готова (попытка {attempt}/5): {ex.Message}");
            if (attempt == 5) throw;
            await Task.Delay(3000);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSvelteApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
