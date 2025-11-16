using System;
using System.Collections.Generic;

namespace PRN232_Quizlet.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<FlashcardSet> FlashcardSets { get; set; } = new List<FlashcardSet>();

    public virtual ICollection<QuizHistory> QuizHistories { get; set; } = new List<QuizHistory>();
}
