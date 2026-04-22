namespace MatinPower.Server.Models.Body
{
    public class LoginRequest
    {
        public string Mobile { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string FullName { get; set; } = null!;
    }
}
