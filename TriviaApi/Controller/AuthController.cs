using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TriviaApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly TriviaDbContext db;
        private const int maxFailedLoginAttempts = 3;

        public AuthController(TriviaDbContext db)
        {
            this.db = db;
        }

        // ========= DTOs coming from Unity =========

        public class RegisterDto
        {
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }

        public class LoginDto
        {
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }

        public class ErrorRes
        {
            public string message { get; set; } = string.Empty;
        }

        public class LoginSuccessRes
        {
            public string username { get; set; } = string.Empty;
        }

        // ================== REGISTER ==================

        // POST /auth/register
        [HttpPost("register")]
        public async Task<IActionResult> register([FromBody] RegisterDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.username) || string.IsNullOrEmpty(dto.password))
            {
                return BadRequest(new ErrorRes { message = "Username and password are required." });
            }

            var username = dto.username.Trim();

            var exists = await db.Users.AnyAsync(u => u.Username == username);
            if (exists)
            {
                return Conflict(new ErrorRes { message = "That username is already taken." });
            }

            // Create salt + hash
            byte[] salt;
            byte[] hash;

            using (var hmac = new HMACSHA512())
            {
                salt = hmac.Key;
                hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.password));
            }

            var user = new User
            {
                Username         = username,
                PasswordSalt     = salt,
                PasswordHash     = hash,
                FailedLoginCount = 0,
                IsLocked         = false
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Ok(new ErrorRes { message = "User registered." });
        }

        // ================== LOGIN ==================

        // POST /auth/login
        [HttpPost("login")]
        public async Task<IActionResult> login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.username) || string.IsNullOrEmpty(dto.password))
            {
                return BadRequest(new ErrorRes { message = "Username and password are required." });
            }

            var username = dto.username.Trim();

            var user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);

            // If user doesn't exist, just say "invalid"
            if (user == null)
            {
                return Unauthorized(new ErrorRes { message = "Invalid username or password." });
            }

            // If already locked, do not process password
            if (user.IsLocked)
            {
                return Unauthorized(new ErrorRes
                {
                    message = "This account has been locked due to too many failed login attempts."
                });
            }

            // Verify password
            bool passwordMatches;
            using (var hmac = new HMACSHA512(user.PasswordSalt))
            {
                var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.password));
                passwordMatches = computed.SequenceEqual(user.PasswordHash);
            }

            if (!passwordMatches)
            {
                user.FailedLoginCount++;

                if (user.FailedLoginCount >= maxFailedLoginAttempts)
                {
                    user.IsLocked = true;
                }

                await db.SaveChangesAsync();

                if (user.IsLocked)
                {
                    return Unauthorized(new ErrorRes
                    {
                        message = "This account has been locked due to too many failed login attempts."
                    });
                }

                return Unauthorized(new ErrorRes { message = "Invalid username or password." });
            }

            // Successful login: reset failed count
            user.FailedLoginCount = 0;
            await db.SaveChangesAsync();

            var res = new LoginSuccessRes
            {
                username = user.Username
            };

            return Ok(res);
        }
    }
}
