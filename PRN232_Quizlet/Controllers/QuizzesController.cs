using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Quizlet.Models;
using static PRN232_Quizlet.DTOs.QuizDtos;

namespace PRN232_Quizlet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizzesController : ControllerBase
    {
        private readonly Prn232QuizletContext _context;

        public QuizzesController(Prn232QuizletContext context)
        {
            _context = context;
        }

        [HttpGet("generate/{setId}")]
        public async Task<IActionResult> GenerateQuiz(int setId, [FromQuery] bool shuffle = false)
        {
            var flashcardsQuery = _context.Flashcards
                .Where(f => f.SetId == setId)
                .Select(f => new
                {
                    f.FlashcardId,
                    f.Question,
                    f.OptionA,
                    f.OptionB,
                    f.OptionC,
                    f.OptionD
                });

            var flashcards = await flashcardsQuery.ToListAsync();

            if (!flashcards.Any())
                return NotFound("Không có câu hỏi nào trong bộ này.");

            // Xáo trộn nếu shuffle = true
            if (shuffle)
            {
                var rng = new Random();
                flashcards = flashcards.OrderBy(_ => rng.Next()).ToList();
            }

            return Ok(flashcards);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] QuizSubmitDto dto)
        {
            var flashcards = await _context.Flashcards
                .Where(f => f.SetId == dto.SetId)
                .ToListAsync();

            int total = flashcards.Count;
            int correct = flashcards.Count(f =>
                dto.Answers.Any(a =>
                    a.FlashcardId == f.FlashcardId &&
                    a.SelectedOption.Equals(f.CorrectOption, StringComparison.OrdinalIgnoreCase)));

            double score = Math.Round((double)correct / total * 10, 2);

            var quizHistory = new QuizHistory
            {
                UserId = dto.UserId,
                SetId = dto.SetId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                TotalQuestions = total,
                CorrectAnswers = correct,
                Score = score
            };

            _context.QuizHistories.Add(quizHistory);
            await _context.SaveChangesAsync();

            var result = new QuizResultDto
            {
                TotalQuestions = total,
                CorrectAnswers = correct,
                Score = score
            };

            return Ok(result);
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetUserHistory(int userId)
        {
            var histories = await _context.QuizHistories
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.EndTime)
                .Select(q => new
                {
                    q.QuizId,
                    q.SetId,
                    q.TotalQuestions,
                    q.CorrectAnswers,
                    q.Score,
                    q.StartTime,
                    q.EndTime
                })
                .ToListAsync();

            return Ok(histories);
        }
    }
}
