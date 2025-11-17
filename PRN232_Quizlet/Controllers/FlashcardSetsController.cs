using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Quizlet.Models;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class FlashcardSetsController : ControllerBase
{
    private readonly Prn232QuizletContext _context;

    public FlashcardSetsController(Prn232QuizletContext context)
    {
        _context = context;
    }

    // =====================================================================
    // GET ALL SETS (Status = Active)
    // =====================================================================
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

        var data = query
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
            data
        });
    }

    // =====================================================================
    // GET SET BY ID + FLASHCARDS (version mới nhất + đang Active)
    // =====================================================================
    [HttpGet("{id}")]
    public IActionResult GetSetById(int id)
    {
        var set = _context.FlashcardSets
            .Include(s => s.CreatedByNavigation)
            .FirstOrDefault(s => s.SetId == id && s.Status == "Active");

        if (set == null)
            return NotFound("Set not found or inactive.");

        // JOIN lấy flashcards version mới nhất + Active
        var flashcards = (
            from cv in _context.FlashcardCurrentVersions
            join f in _context.Flashcards
                on new { cv.FlashcardId, Version = cv.CurrentVersion }
                equals new { f.FlashcardId, Version = f.Version }
            where f.SetId == id && f.Status == "Active"
            select new
            {
                f.FlashcardId,
                f.Version,
                f.Question,
                f.OptionA,
                f.OptionB,
                f.OptionC,
                f.OptionD,
                f.CorrectOption
            }
        ).ToList();

        return Ok(new
        {
            set.SetId,
            set.Title,
            set.Description,
            set.StudyCount,
            set.CreatedAt,
            Author = set.CreatedByNavigation.FullName,
            Flashcards = flashcards
        });
    }

    // =====================================================================
    // GET MY SETS
    // =====================================================================
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

    // =====================================================================
    // CREATE SET
    // =====================================================================
    [Authorize]
    [HttpPost]
    public IActionResult CreateSet([FromBody] FlashcardSet model)
    {
        var userId = int.Parse(User.FindFirstValue("UserID")!);

        var set = new FlashcardSet
        {
            Title = model.Title,
            Description = model.Description,
            CreatedBy = userId,
            StudyCount = 0,
            CreatedAt = DateTime.UtcNow,
            Status = "Active"
        };

        _context.FlashcardSets.Add(set);
        _context.SaveChanges();

        return Ok(new { message = "Set created successfully.", set.SetId });
    }

    // =====================================================================
    // UPDATE SET
    // =====================================================================
    [Authorize]
    [HttpPut("{id}")]
    public IActionResult UpdateSet(int id, [FromBody] FlashcardSet model)
    {
        var userId = int.Parse(User.FindFirstValue("UserID")!);

        var set = _context.FlashcardSets.FirstOrDefault(s => s.SetId == id && s.Status == "Active");

        if (set == null)
            return NotFound("Set not found or inactive.");
        if (set.CreatedBy != userId)
            return Forbid("You are not the owner.");

        set.Title = model.Title;
        set.Description = model.Description;

        _context.SaveChanges();

        return Ok(new { message = "Set updated successfully." });
    }

    // =====================================================================
    // DELETE SET (Soft delete)
    // =====================================================================
    [Authorize]
    [HttpDelete("{id}")]
    public IActionResult DeleteSet(int id)
    {
        var userId = int.Parse(User.FindFirstValue("UserID")!);

        var set = _context.FlashcardSets.FirstOrDefault(s => s.SetId == id && s.Status == "Active");

        if (set == null)
            return NotFound("Set not found or already inactive.");
        if (set.CreatedBy != userId)
            return Forbid("You are not the owner.");

        // Soft delete
        set.Status = "Inactive";

        // Optional: cũng inactive Flashcards của set (nếu cần)
        var cards = _context.Flashcards.Where(f => f.SetId == id);
        foreach (var card in cards)
            card.Status = "Inactive";

        _context.SaveChanges();

        return Ok(new { message = "Set deleted (soft)." });
    }
}
