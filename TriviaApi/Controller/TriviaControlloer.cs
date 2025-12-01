using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TriviaApi.Controllers
{
    // trivia controller

    [ApiController]
    [Route("trivia")]
    public class TriviaController : ControllerBase
    {
        private readonly TriviaDbContext db;
        private static readonly Random rng = new Random();

        public TriviaController(TriviaDbContext db)
        {
            this.db = db;
        }

        [HttpGet("next")]
        [AllowAnonymous]
        public async Task<ActionResult<QuestionRes>> next()
        {
            var activeCount = await db.Questions
                .AsNoTracking()
                .Where(q => q.Active)
                .CountAsync();

            if (activeCount == 0)
            {
                return NotFound(new { message = "No active questions." });
            }

            int skip = rng.Next(0, activeCount);

            var q = await db.Questions
                .AsNoTracking()
                .Where(x => x.Active)
                .OrderBy(x => x.Id)
                .Skip(skip)
                .FirstOrDefaultAsync();

            if (q == null)
            {
                return NotFound(new { message = "No question found." });
            }

            string[] choices;
            try
            {
                choices = JsonSerializer.Deserialize<string[]>(q.ChoicesJson ?? "[]")
                           ?? Array.Empty<string>();
            }
            catch
            {
                return Problem($"Invalid ChoicesJSON for question {q.Id}.");
            }

            if (choices.Length == 0)
            {
                return Problem($"Question {q.Id} has no choices.");
            }

            var res = new QuestionRes
            {
                questionId = q.Id,
                question   = q.Text,
                choices    = choices
            };

            return Ok(res);
        }

        [HttpPost("answer")]
        [AllowAnonymous]
        public async Task<ActionResult<AnswerRes>> answer([FromBody] AnswerReq req)
        {
            if (req == null)
            {
                return BadRequest(new { message = "Body required." });
            }

            var q = await db.Questions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == req.questionId);

            if (q == null)
            {
                return BadRequest(new { message = "Unknown question." });
            }

            var key = await db.AnswerKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.QuestionId == q.Id);

            if (key == null)
            {
                return Problem($"No answer key for question {q.Id}.");
            }

            string[] choices;
            try
            {
                choices = JsonSerializer.Deserialize<string[]>(q.ChoicesJson ?? "[]")
                           ?? Array.Empty<string>();
            }
            catch
            {
                return Problem($"Invalid ChoicesJSON for question {q.Id}.");
            }

            if (choices.Length == 0)
            {
                return Problem($"Question {q.Id} has no choices.");
            }

            var correctIndex = key.CorrectChoiceIndex;

            if (correctIndex < 0 || correctIndex >= choices.Length)
            {
                return Problem($"CorrectChoiceIndex out of range for question {q.Id}.");
            }

            bool isCorrect = req.choiceIndex == correctIndex;

            var res = new AnswerRes
            {
                correct       = isCorrect,
                correctIndex  = correctIndex,
                correctAnswer = choices[correctIndex]
            };

            return Ok(res);
        }

        [HttpPost("end")]
        [AllowAnonymous]
        public IActionResult end()
        {
            return Ok(new { message = "ended" });
        }
    }

    public class TriviaDbContext : DbContext
    {
        public TriviaDbContext(DbContextOptions<TriviaDbContext> options)
            : base(options)
        {
        }

        public DbSet<Question>  Questions  => Set<Question>();
        public DbSet<AnswerKey> AnswerKeys => Set<AnswerKey>();
        public DbSet<User>      Users      => Set<User>();  

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Question>(e =>
            {
                e.ToTable("Questions", "dbo");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                    .HasColumnName("Id");

                e.Property(x => x.Text)
                    .HasColumnName("Text");

                e.Property(x => x.ChoicesJson)
                    .HasColumnName("ChoicesJSON");

                e.Property(x => x.Active)
                    .HasColumnName("Active");
            });

            b.Entity<AnswerKey>(e =>
            {
                e.ToTable("Answers", "dbo");

                e.HasKey(x => x.QuestionId);

                e.Property(x => x.QuestionId)
                    .HasColumnName("QuestionId");

                e.Property(x => x.CorrectChoiceIndex)
                    .HasColumnName("CorrectChoiceIndex");
            });

            b.Entity<User>(e =>
            {
                e.ToTable("Users", "dbo");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                    .HasColumnName("Id");

                e.Property(x => x.Username)
                    .HasColumnName("Username");

                e.Property(x => x.PasswordHash)
                    .HasColumnName("PasswordHash");

                e.Property(x => x.PasswordSalt)
                    .HasColumnName("PasswordSalt");

                e.Property(x => x.FailedLoginCount)
                    .HasColumnName("FailedLoginCount");

                e.Property(x => x.IsLocked)
                    .HasColumnName("IsLocked");

                e.Property(x => x.TotalGamesPlayed)
                    .HasColumnName("TotalGamesPlayed");

                e.Property(x => x.TotalQuestionsAnswered)
                    .HasColumnName("TotalQuestionsAnswered");

                e.Property(x => x.TotalCorrectAnswers)
                    .HasColumnName("TotalCorrectAnswers");
            });
        }
    }

    public class Question
    {
        public int    Id          { get; set; }
        public string Text        { get; set; } = "";
        public string ChoicesJson { get; set; } = "[]"; 
        public bool   Active      { get; set; }
    }

    public class AnswerKey
    {
        public int  QuestionId         { get; set; } 
        public byte CorrectChoiceIndex { get; set; } 
    }

    public class User
    {
        public int    Id               { get; set; }
        public string Username         { get; set; } = string.Empty;
        public byte[] PasswordHash     { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt     { get; set; } = Array.Empty<byte>();
        public int    FailedLoginCount { get; set; }
        public bool   IsLocked         { get; set; }

        // per-user stats
        public int TotalGamesPlayed       { get; set; }
        public int TotalQuestionsAnswered { get; set; }
        public int TotalCorrectAnswers    { get; set; }
    }

    // dtos

    public class QuestionRes
    {
        public int      questionId { get; set; }
        public string   question   { get; set; } = "";
        public string[] choices    { get; set; } = Array.Empty<string>();
    }

    public class AnswerReq
    {
        public int questionId  { get; set; }
        public int choiceIndex { get; set; }
    }

    public class AnswerRes
    {
        public bool   correct       { get; set; }
        public int    correctIndex  { get; set; }
        public string correctAnswer { get; set; } = "";
    }
}