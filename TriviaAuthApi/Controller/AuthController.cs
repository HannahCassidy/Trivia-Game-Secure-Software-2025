using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TriviaAuthApi.Controller
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IJwtService jwt;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IJwtService jwt)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.jwt = jwt;
        }

        // -------- DTOs (camelCase) --------
        public class RegisterReq
        {
            public string? username { get; set; }
            public string? password { get; set; }
        }

        public class LoginReq
        {
            public string? username { get; set; }
            public string? password { get; set; }
        }

        // POST /auth/register
        [HttpPost("register")]
        public async Task<IActionResult> register([FromBody] RegisterReq req)
        {
            if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.password))
                return BadRequest(new { error = "username and password are required" });

            // ensure username is unique
            var existing = await userManager.FindByNameAsync(req.username);
            if (existing != null)
                return BadRequest(new { error = "username is already taken" });

            var user = new IdentityUser
            {
                UserName = req.username,
                // Email is intentionally not used anywhere
            };

            var result = await userManager.CreateAsync(user, req.password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(new { errors });
            }

            // optional: sign-in immediately & return token so the game can proceed
            var token = jwt.Create(user);
            return Ok(new { token });
        }

        // POST /auth/login
        [HttpPost("login")]
        public async Task<IActionResult> login([FromBody] LoginReq req)
        {
            if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.password))
                return BadRequest(new { error = "username and password are required" });

            var user = await userManager.FindByNameAsync(req.username);
            if (user == null)
                return Unauthorized();

            // lockoutOnFailure ties into your Identity options (MaxFailedAccessAttempts, etc.)
            var result = await signInManager.CheckPasswordSignInAsync(user, req.password, lockoutOnFailure: true);
            if (!result.Succeeded)
                return Unauthorized();

            // IMPORTANT: return the raw JWT string (header.payload.signature) without altering it
            var token = jwt.Create(user);
            return Ok(new { token });
        }
    }
}
