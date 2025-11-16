using System;
using System.Collections.Generic;

namespace PRN232_Quizlet.Models;

public partial class QuizAttemptDetail
{
    public int DetailId { get; set; }

    public int QuizId { get; set; }

    public int FlashcardId { get; set; }

    public int Version { get; set; }

    public string? UserAnswer { get; set; }

    public bool? IsCorrect { get; set; }

    public virtual Flashcard Flashcard { get; set; } = null!;

    public virtual QuizHistory Quiz { get; set; } = null!;
}
