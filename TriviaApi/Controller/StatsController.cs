using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TriviaApi.Controllers
{
    [ApiController]
    [Route("stats")]
    public class StatsController : ControllerBase
    {
        private readonly TriviaDbContext db;

        public StatsController(TriviaDbContext db)
        {
            this.db = db;
        }

        public class GameResultDto
        {
            public string Username       { get; set; } = string.Empty;
            public int    TotalQuestions { get; set; }
            public int    CorrectAnswers { get; set; }
        }

        public class UserStatsRes
        {
            public int    TotalGamesPlayed       { get; set; }
            public int    TotalQuestionsAnswered { get; set; }
            public int    TotalCorrectAnswers    { get; set; }
            public double Accuracy               { get; set; }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] GameResultDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Username) ||
                dto.TotalQuestions <= 0 ||
                dto.CorrectAnswers < 0 ||
                dto.CorrectAnswers > dto.TotalQuestions)
            {
                return BadRequest(new { message = "Invalid game result." });
            }

            var username = dto.Username.Trim();

            var user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return BadRequest(new { message = "Unknown user." });
            }

            user.TotalGamesPlayed       += 1;
            user.TotalQuestionsAnswered += dto.TotalQuestions;
            user.TotalCorrectAnswers    += dto.CorrectAnswers;

            await db.SaveChangesAsync();   

            return Ok(new { message = "Stats updated." });
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<UserStatsRes>> GetForUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username required." });

            var user = await db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Username == username.Trim());

            if (user == null)
                return NotFound(new { message = "User not found." });

            double accuracy = user.TotalQuestionsAnswered == 0
                ? 0
                : (double)user.TotalCorrectAnswers / user.TotalQuestionsAnswered;

            var res = new UserStatsRes
            {
                TotalGamesPlayed       = user.TotalGamesPlayed,
                TotalQuestionsAnswered = user.TotalQuestionsAnswered,
                TotalCorrectAnswers    = user.TotalCorrectAnswers,
                Accuracy               = accuracy
            };

            return Ok(res);
        }
    }
}
