using VetClinicBackend.Auth;
using VetClinicBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VetClinicBackend.Controllers
{
    public record LoginRequest(string Username, string Password);

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokens;

        public AuthController(AppDbContext context, TokenService tokens)
        {
            _context = context;
            _tokens = tokens;
        }

        /// <summary>Перевод кода роли бэкенда в строку, ожидаемую фронтендом.</summary>
        private static string MapRole(string code) => code switch
        {
            "Director"     => "director",
            "ChiefVet"     => "chief_vet",
            "Vet"          => "vet",
            "Receptionist" => "receptionist",
            _              => code.ToLowerInvariant()
        };

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.Employee).ThenInclude(e => e.Position)
                .Include(u => u.Employee).ThenInclude(e => e.Branch)
                .FirstOrDefaultAsync(u => u.Login == req.Username);

            if (user == null || user.IsLocked || !PasswordHasher.Verify(req.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный логин или пароль" });

            var roleCodes = user.UserRoles.Select(ur => ur.Role.Code).ToList();
            var token = _tokens.CreateToken(user, roleCodes);

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var emp = user.Employee;
            return Ok(new
            {
                token,
                user = new
                {
                    id = $"usr-{user.UserId}",
                    username = user.Login,
                    employeeId = $"emp-{user.EmployeeId}",
                    roles = roleCodes.Select(MapRole).ToArray(),
                    status = user.IsLocked ? "inactive" : "active"
                },
                employee = new
                {
                    id = $"emp-{emp.EmployeeId}",
                    name = emp.FullName,
                    positionId = $"pos-{emp.PositionId}",
                    phone = emp.Phone,
                    email = emp.Email,
                    KPIRate = emp.KPIRate ?? 0,
                    branchIds = new[] { $"br-{emp.BranchId}" },
                    status = emp.IsActive ? "active" : "inactive"
                }
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                login = User.FindFirst("login")?.Value,
                employeeId = User.FindFirst("employeeId")?.Value,
                roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => MapRole(c.Value)).ToArray()
            });
        }
    }
}
