using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRN232_Quizlet.Models;

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
        public IActionResult GetAll()
        {
            var sets = _context.FlashcardSets
                .Select(s => new
                {
                    s.SetId,
                    s.Title,
                    s.Description,
                    s.StudyCount,
                    CreatedBy = _context.Users
                        .Where(u => u.UserId == s.CreatedBy)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                })
                .ToList();

            return Ok(sets);
        }
    }
}

