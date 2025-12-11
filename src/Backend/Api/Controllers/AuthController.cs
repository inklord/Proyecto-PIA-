using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Models;
using MySql.Data.MySqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Persistence;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Registro sencillo: crea usuario directamente sin verificar correo
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email y contraseña son obligatorios.");

            var connStr = RepositoryFactory.CurrentConnectionString;
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE email = @Email", conn);
            checkCmd.Parameters.AddWithValue("@Email", request.Email);
            var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;
            if (exists)
                return Conflict("Ya existe un usuario con ese email.");

            var insertCmd = new MySqlCommand(
                "INSERT INTO users (email, password_hash, role, is_verified) " +
                "VALUES (@Email, @Pass, 'User', 1)", conn);
            insertCmd.Parameters.AddWithValue("@Email", request.Email);
            insertCmd.Parameters.AddWithValue("@Pass", request.Password); // Para demo guardamos en claro
            await insertCmd.ExecuteNonQueryAsync();

            return Ok("Usuario registrado correctamente. Ya puedes iniciar sesión.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            var connStr = RepositoryFactory.CurrentConnectionString;
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(
                "SELECT email, password_hash FROM users WHERE email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", login.Email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Unauthorized("Usuario o contraseña incorrectos.");

            var dbPass = reader["password_hash"] as string ?? "";

            if (dbPass != login.Password)
                return Unauthorized("Usuario o contraseña incorrectos.");

            var email = reader["email"].ToString() ?? "";

            var token = GenerateJwtToken(email);
            return Ok(new LoginResponse
            {
                Token = token,
                Email = email,
                Expiration = DateTime.UtcNow.AddHours(1)
            });
        }

        private string GenerateJwtToken(string email)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, email) }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
