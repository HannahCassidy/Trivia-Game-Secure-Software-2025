using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TriviaApi.Controllers
{
    // ===================== TRIVIA CONTROLLER =====================

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

        // GET /trivia/next  -> returns a random active question
        [HttpGet("next")]
        [AllowAnonymous]
        public async Task<IActionResult> next()
        {
            var count = await db.Questions.Where(q => q.Active).CountAsync();
            if (count == 0)
            {
                return Problem("No active questions in database.");
            }

            var skip = rng.Next(0, count);

            var q = await db.Questions
                .Where(x => x.Active)
                .OrderBy(x => x.Id)
                .Skip(skip)
                .Take(1)
                .AsNoTracking()
                .FirstAsync();

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
                question   = q.Text ?? string.Empty,
                choices    = choices
            };

            return Ok(res);
        }

        // POST /trivia/answer  -> checks if selected answer is correct
        [HttpPost("answer")]
        [AllowAnonymous]
        public async Task<IActionResult> answer([FromBody] AnswerReq req)
        {
            if (req == null)
            {
                return BadRequest(new { message = "Missing body." });
            }

            var q = await db.Questions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == req.questionId);

            if (q == null)
            {
                return BadRequest(new { message = "Unknown question." });
            }

            var choices = JsonSerializer.Deserialize<string[]>(q.ChoicesJson ?? "[]")
                           ?? Array.Empty<string>();

            if (choices.Length == 0)
            {
                return Problem($"Question {q.Id} has no choices.");
            }

            var key = await db.AnswerKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.QuestionId == req.questionId);

            if (key == null)
            {
                return Problem($"No answer key for question {q.Id}.");
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

        // POST /trivia/end  -> simple “end game” hook
        [HttpPost("end")]
        [AllowAnonymous]
        public IActionResult end()
        {
            return Ok(new { message = "ended" });
        }
    }

    // ===================== DB CONTEXT =====================

    public class TriviaDbContext : DbContext
    {
        public TriviaDbContext(DbContextOptions<TriviaDbContext> options)
            : base(options)
        {
        }

        public DbSet<Question>  Questions  => Set<Question>();
        public DbSet<AnswerKey> AnswerKeys => Set<AnswerKey>();
        public DbSet<User>      Users      => Set<User>();   // <- needed by AuthController

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Map Questions -> dbo.Questions
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

            // Map AnswerKey -> existing dbo.Answers table
            b.Entity<AnswerKey>(e =>
            {
                // IMPORTANT: matches your actual table name
                e.ToTable("Answers", "dbo");

                // There is no Id column; use QuestionId as key
                e.HasKey(x => x.QuestionId);

                e.Property(x => x.QuestionId)
                    .HasColumnName("QuestionId");

                e.Property(x => x.CorrectChoiceIndex)
                    .HasColumnName("CorrectChoiceIndex");
            });

            // Map User -> dbo.Users (created by AddUsers migration)
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
            });
        }
    }

    // ===================== ENTITIES =====================

    public class Question
    {
        public int    Id          { get; set; }
        public string Text        { get; set; } = "";
        public string ChoicesJson { get; set; } = "[]";
        public bool   Active      { get; set; }
    }

    // Maps to dbo.Answers
    public class AnswerKey
    {
        public int  QuestionId         { get; set; } // PK
        public byte CorrectChoiceIndex { get; set; } // tinyint -> byte
    }

    // Maps to dbo.Users
    public class User
    {
        public int    Id               { get; set; }
        public string Username         { get; set; } = string.Empty;
        public byte[] PasswordHash     { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt     { get; set; } = Array.Empty<byte>();
        public int    FailedLoginCount { get; set; }
        public bool   IsLocked         { get; set; }
    }

    // ===================== DTOs =====================

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
