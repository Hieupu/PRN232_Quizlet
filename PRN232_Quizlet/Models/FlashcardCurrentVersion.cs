using System;
using System.Collections.Generic;

namespace PRN232_Quizlet.Models;

public partial class FlashcardCurrentVersion
{
    public int FlashcardId { get; set; }

    public int CurrentVersion { get; set; }

    public virtual Flashcard Flashcard { get; set; } = null!;
}
