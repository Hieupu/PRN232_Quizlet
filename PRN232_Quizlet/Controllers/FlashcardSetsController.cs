using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Quizlet.Models;
using System.Security.Claims;

namespace PRN232_Quizlet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlashcardSetsController : ControllerBase
    {
        private readonly Prn232QuizletContext _context;

        public FlashcardSetsController(Prn232QuizletContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAllSets(string? search = "", int page = 1, int pageSize = 8)
        {
            var query = _context.FlashcardSets
                .Include(s => s.CreatedByNavigation)
                .Where(s => s.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.Title.Contains(search));

            var total = query.Count();

            var sets = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.SetId,
                    s.Title,
                    s.Description,
                    s.StudyCount,
                    s.CreatedAt,
                    Author = s.CreatedByNavigation.FullName
                })
                .ToList();

            return Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                data = sets
            });
        }

        [HttpGet("{id}")]
        public IActionResult GetSetById(int id)
        {
            var set = _context.FlashcardSets
                .Include(s => s.CreatedByNavigation)
                .Include(s => s.Flashcards)
                .FirstOrDefault(s => s.SetId == id && s.Status == "Active");

            if (set == null)
                return NotFound("Set not found or inactive.");

            return Ok(new
            {
                set.SetId,
                set.Title,
                set.Description,
                set.StudyCount,
                set.CreatedAt,
                Author = set.CreatedByNavigation.FullName,
                Flashcards = set.Flashcards
                    .Where(f => f.Status == "Active")
                    .Select(f => new
                    {
                        f.FlashcardId,
                        f.Question,
                        f.OptionA,
                        f.OptionB,
                        f.OptionC,
                        f.OptionD,
                        f.CorrectOption
                    })
            });
        }

        [Authorize]
        [HttpGet("mysets")]
        public IActionResult GetMySets()
        {
            var userId = int.Parse(User.FindFirstValue("UserID")!);

            var sets = _context.FlashcardSets
                .Where(s => s.CreatedBy == userId && s.Status == "Active")
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.SetId,
                    s.Title,
                    s.Description,
                    s.StudyCount,
                    s.CreatedAt
                })
                .ToList();

            return Ok(sets);
        }

        [Authorize]
        [HttpPost]
        public IActionResult CreateSet([FromBody] FlashcardSet model)
        {
            var userId = int.Parse(User.FindFirstValue("UserID")!);

            var newSet = new FlashcardSet
            {
                Title = model.Title,
                Description = model.Description,
                CreatedBy = userId,
                StudyCount = 0,
                CreatedAt = DateTime.UtcNow,
                Status = "Active"
            };

            _context.FlashcardSets.Add(newSet);
            _context.SaveChanges();

            return Ok(new { message = "Set created successfully.", newSet.SetId });
        }

        [Authorize]
        [HttpPut("{id}")]
        public IActionResult UpdateSet(int id, [FromBody] FlashcardSet model)
        {
            var userId = int.Parse(User.FindFirstValue("UserID")!);
            var set = _context.FlashcardSets.FirstOrDefault(s => s.SetId == id && s.Status == "Active");

            if (set == null)
                return NotFound("Set not found or inactive.");
            if (set.CreatedBy != userId)
                return Forbid("You are not the owner of this set.");

            set.Title = model.Title;
            set.Description = model.Description;
            _context.SaveChanges();

            return Ok(new { message = "Set updated successfully." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult DeleteSet(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserID")!);
            var set = _context.FlashcardSets
                .Include(s => s.Flashcards)
                .FirstOrDefault(s => s.SetId == id && s.Status == "Active");

            if (set == null)
                return NotFound("Set not found or already inactive.");
            if (set.CreatedBy != userId)
                return Forbid("You are not the owner of this set.");

            set.Status = "Inactive";

            foreach (var f in set.Flashcards)
                f.Status = "Inactive";

            _context.SaveChanges();

            return Ok(new { message = "Set marked as inactive (soft deleted)." });
        }
    }
}
