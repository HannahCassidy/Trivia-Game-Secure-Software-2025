using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TriviaApi.Controllers
{
    [ApiController]
    [Route("trivia")]
    public class TriviaController : ControllerBase
    {
        private readonly TriviaDbContext db;
        private static readonly Random rng = new Random();

        public TriviaController(TriviaDbContext db) => this.db = db;

        [HttpGet("next")]
        [AllowAnonymous]
        public async Task<IActionResult> next()
        {
            var count = await db.Questions.Where(q => q.Active).CountAsync();
            if (count == 0) return Problem("No active questions in database.");

            var skip = rng.Next(0, count);
            var q = await db.Questions.Where(x => x.Active)
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

            if (choices.Length == 0) return Problem($"Question {q.Id} has no choices.");

            var res = new QuestionRes
            {
                questionId = q.Id,
                question   = q.Text ?? "",
                choices    = choices
            };
            return Ok(res);
        }

        [HttpPost("answer")]
        [AllowAnonymous]
        public async Task<IActionResult> answer([FromBody] AnswerReq req)
        {
            if (req is null) return BadRequest(new { message = "Missing body." });

            var q = await db.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.questionId);
            if (q is null) return BadRequest(new { message = "Unknown question." });

            var choices = JsonSerializer.Deserialize<string[]>(q.ChoicesJson ?? "[]") ?? Array.Empty<string>();
            if (choices.Length == 0) return Problem($"Question {q.Id} has no choices.");

            var key = await db.AnswerKeys.AsNoTracking()
                           .FirstOrDefaultAsync(a => a.QuestionId == req.questionId);
            if (key is null) return Problem($"No answer key for question {q.Id}.");

            var correctIndex = key.CorrectChoiceIndex;
            if (correctIndex < 0 || correctIndex >= choices.Length)
                return Problem($"CorrectChoiceIndex out of range for question {q.Id}.");

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
        public IActionResult end() => Ok(new { message = "ended" });
    }

    public class TriviaDbContext : DbContext
    {
        public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options) { }

        public DbSet<Question>  Questions  => Set<Question>();
        public DbSet<AnswerKey> AnswerKeys => Set<AnswerKey>();

        protected override void OnModelCreating(ModelBuilder b)
        {
         
            b.Entity<Question>(e =>
            {
                e.ToTable("Questions", "dbo");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id)         .HasColumnName("Id");
                e.Property(x => x.Text)       .HasColumnName("Text");
                e.Property(x => x.ChoicesJson).HasColumnName("ChoicesJSON");
                e.Property(x => x.Active)     .HasColumnName("Active");
            });

            b.Entity<AnswerKey>(e =>
            {
                e.ToTable("Answers", "dbo");
                e.HasKey(x => x.QuestionId); 
                e.Property(x => x.QuestionId)        .HasColumnName("QuestionId");
                e.Property(x => x.CorrectChoiceIndex).HasColumnName("CorrectChoiceIndex");
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

    // DTOs
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
