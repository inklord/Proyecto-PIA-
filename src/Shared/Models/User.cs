namespace Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // En producción usar hash real
        public string Role { get; set; } = "User";
        public bool IsVerified { get; set; }
        public string? VerificationCode { get; set; }
        public DateTime? VerificationExpiresAt { get; set; }
    }
}
