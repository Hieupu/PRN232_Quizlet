using System;
using System.Collections.Generic;

namespace PRN232_Quizlet.Models;

public partial class QuizHistory
{
    public int QuizId { get; set; }

    public int? UserId { get; set; }

    public int? SetId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public double Score { get; set; }

    public virtual ICollection<QuizAttemptDetail> QuizAttemptDetails { get; set; } = new List<QuizAttemptDetail>();

    public virtual FlashcardSet? Set { get; set; }

    public virtual User? User { get; set; }
}
