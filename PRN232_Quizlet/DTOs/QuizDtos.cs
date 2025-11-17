namespace PRN232_Quizlet.DTOs
{
    public class QuizDtos
    {
        public class QuizSubmitDto
        {
            public int UserId { get; set; }
            public int SetId { get; set; }
            public List<AnswerItem> Answers { get; set; } = new();
        }

        public class AnswerItem
        {
            public int FlashcardId { get; set; }
            public string SelectedOption { get; set; } = string.Empty;
        }

        public class QuizResultDto
        {
            public int TotalQuestions { get; set; }
            public int CorrectAnswers { get; set; }
            public double Score { get; set; }
        }


        public class AnswerDto
        {
            public int FlashcardId { get; set; }
            public string SelectedOption { get; set; }
        }

    }
}
