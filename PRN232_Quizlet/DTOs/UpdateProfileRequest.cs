namespace PRN232_Quizlet.DTOs
{
    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? Password { get; set; }
        public string? CurrentPassword { get; set; }
    }
}

