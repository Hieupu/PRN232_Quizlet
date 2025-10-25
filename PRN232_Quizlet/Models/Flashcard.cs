using System;
using System.Collections.Generic;

namespace PRN232_Quizlet.Models;

public partial class Flashcard
{
    public int FlashcardId { get; set; }

    public int SetId { get; set; }

    public string Question { get; set; } = null!;

    public string OptionA { get; set; } = null!;

    public string OptionB { get; set; } = null!;

    public string OptionC { get; set; } = null!;

    public string OptionD { get; set; } = null!;

    public string CorrectOption { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual FlashcardSet Set { get; set; } = null!;
}
