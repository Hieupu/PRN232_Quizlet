using System;
using System.Collections.Generic;

namespace PRN232_Quizlet.Models;

public partial class FlashcardSet
{
    public int SetId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int CreatedBy { get; set; }

    public int? StudyCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();

    public virtual ICollection<QuizHistory> QuizHistories { get; set; } = new List<QuizHistory>();
}
