using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AhillBackend.Models;
using Microsoft.IdentityModel.Tokens;

namespace AhillBackend.Auth
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config) => _config = config;

        /// <summary>Сгенерировать подписанный JWT с ролями пользователя (RBAC).</summary>
        public string CreateToken(AppUser user, IEnumerable<string> roleCodes)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new("uid", user.UserId.ToString()),
                new("login", user.Login),
                new("employeeId", user.EmployeeId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var code in roleCodes)
                claims.Add(new Claim(ClaimTypes.Role, code));

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"] ?? "480")),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
