namespace Ecommerce.Models.Dto
{
    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public string? Token { get; set; }
        public DateTime? Expiration { get; set; }
        public List<string>? Roles { get; set; }
    }
}
