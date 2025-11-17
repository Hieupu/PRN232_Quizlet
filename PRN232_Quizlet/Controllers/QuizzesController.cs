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
        public async Task<IActionResult> GenerateQuiz(
     int setId,
     [FromQuery] int amount = 0,
     [FromQuery] bool shuffle = false)
        {
            var flashcards = await (
                from cv in _context.FlashcardCurrentVersions
                join f in _context.Flashcards
                    on new { cv.FlashcardId, Version = cv.CurrentVersion }
                    equals new { f.FlashcardId, Version = f.Version }
                where f.SetId == setId && f.Status == "Active"
                select new
                {
                    f.FlashcardId,
                    f.Version,
                    f.Question,
                    f.OptionA,
                    f.OptionB,
                    f.OptionC,
                    f.OptionD
                }
            ).ToListAsync();

            if (!flashcards.Any())
                return NotFound("Không có câu hỏi nào trong bộ này.");

            if (shuffle)
            {
                var rng = new Random();
                flashcards = flashcards.OrderBy(_ => rng.Next()).ToList();
            }

            if (amount > 0 && amount < flashcards.Count)
            {
                var rng = new Random();
                flashcards = flashcards
                    .OrderBy(_ => rng.Next())
                    .Take(amount)
                    .ToList();
            }

            return Ok(new
            {
                total = flashcards.Count,
                questions = flashcards
            });
        }


        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] QuizSubmitDto dto)
        {
            // 1. Lấy version hiện tại của tất cả flashcards trong set
            var currentCards = await (
                from cv in _context.FlashcardCurrentVersions
                join f in _context.Flashcards
                    on new { cv.FlashcardId, Version = cv.CurrentVersion }
                    equals new { f.FlashcardId, Version = f.Version }
                where f.SetId == dto.SetId && f.Status == "Active"
                select f
            ).ToListAsync();

            int total = dto.Answers.Count;
            int correct = 0;

            foreach (var ans in dto.Answers)
            {
                var card = currentCards.FirstOrDefault(f => f.FlashcardId == ans.FlashcardId);
                if (card != null &&
                    card.CorrectOption.Equals(ans.SelectedOption, StringComparison.OrdinalIgnoreCase))
                {
                    correct++;
                }
            }

            double score = Math.Round(correct * 10.0 / total, 2);

            // 2. Save QuizHistory
            var history = new QuizHistory
            {
                UserId = dto.UserId,
                SetId = dto.SetId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                TotalQuestions = total,
                CorrectAnswers = correct,
                Score = score
            };

            _context.QuizHistories.Add(history);
            await _context.SaveChangesAsync();

            // 3. Save từng câu trong QuizAttemptDetail
            foreach (var ans in dto.Answers)
            {
                var card = currentCards.First(f => f.FlashcardId == ans.FlashcardId);

                _context.QuizAttemptDetails.Add(new QuizAttemptDetail
                {
                    QuizId = history.QuizId,
                    FlashcardId = card.FlashcardId,
                    Version = card.Version,
                    UserAnswer = ans.SelectedOption,
                    IsCorrect = card.CorrectOption.Equals(ans.SelectedOption, StringComparison.OrdinalIgnoreCase)
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                total,
                correct,
                score
            });
        }


        [HttpGet("result/{quizId}")]
        public async Task<IActionResult> GetQuizResult(int quizId)
        {
            var history = await _context.QuizHistories
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (history == null)
                return NotFound("Quiz not found.");

            return Ok(new
            {
                history.QuizId,
                history.SetId,
                history.TotalQuestions,
                Correct = history.CorrectAnswers,
                Wrong = history.TotalQuestions - history.CorrectAnswers,
                Score = history.Score,
                Start = history.StartTime,
                End = history.EndTime
            });
        }

        [HttpGet("result-detail/{quizId}")]
        public async Task<IActionResult> GetQuizResultDetail(int quizId)
        {
            var details = await (
                from d in _context.QuizAttemptDetails
                join f in _context.Flashcards
                    on new { d.FlashcardId, d.Version }
                    equals new { f.FlashcardId, Version = f.Version }
                where d.QuizId == quizId
                select new
                {
                    d.FlashcardId,
                    d.Version,
                    f.Question,
                    f.OptionA,
                    f.OptionB,
                    f.OptionC,
                    f.OptionD,
                    CorrectOption = f.CorrectOption,
                    UserAnswer = d.UserAnswer,
                    d.IsCorrect
                }
            ).ToListAsync();

            if (!details.Any())
                return NotFound("No results found.");

            return Ok(details);
        }

        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetUserStats(int userId)
        {
            var histories = await _context.QuizHistories
                .Where(q => q.UserId == userId)
                .ToListAsync();

            if (!histories.Any())
                return NotFound("User has no quiz attempts.");

            int totalQuestions = histories.Sum(h => h.TotalQuestions);
            int totalCorrect = histories.Sum(h => h.CorrectAnswers);
            int totalWrong = totalQuestions - totalCorrect;

            double accuracy = Math.Round((double)totalCorrect / totalQuestions * 100, 2);
            double avgScore = Math.Round(histories.Average(h => h.Score), 2);

            return Ok(new
            {
                TotalQuestions = totalQuestions,
                Correct = totalCorrect,
                Wrong = totalWrong,
                AccuracyPercent = accuracy,
                AverageScore = avgScore,
                TotalQuizAttempts = histories.Count
            });
        }

        [HttpGet("stats/{userId}/set/{setId}")]
        public async Task<IActionResult> GetUserStatsBySet(int userId, int setId)
        {
            var histories = await _context.QuizHistories
                .Where(q => q.UserId == userId && q.SetId == setId)
                .ToListAsync();

            if (!histories.Any())
                return NotFound("User has no quiz attempts for this set.");

            int totalQuestions = histories.Sum(h => h.TotalQuestions);
            int totalCorrect = histories.Sum(h => h.CorrectAnswers);
            int totalWrong = totalQuestions - totalCorrect;

            double accuracy = Math.Round((double)totalCorrect / totalQuestions * 100, 2);
            double avgScore = Math.Round(histories.Average(h => h.Score), 2);

            return Ok(new
            {
                SetId = setId,
                TotalQuestions = totalQuestions,
                Correct = totalCorrect,
                Wrong = totalWrong,
                AccuracyPercent = accuracy,
                AverageScore = avgScore,
                TotalQuizAttempts = histories.Count
            });
        }


        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetUserHistory(int userId)
        {
            var list = await _context.QuizHistories
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.EndTime)
                .Select(q => new
                {
                    q.QuizId,
                    q.SetId,
                    q.TotalQuestions,
                    q.CorrectAnswers,
                    Wrong = q.TotalQuestions - q.CorrectAnswers,
                    q.Score,
                    q.StartTime,
                    q.EndTime
                })
                .ToListAsync();

            return Ok(list);
        }



    }
}
