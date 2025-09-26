namespace Ecommerce.Models.Dto
{
    public enum UserRole
    {
        User,
        Admin
    }

    public class RegisterDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        private string _role = "User";
        public string Role
        {
            get => string.IsNullOrWhiteSpace(_role) ? "User" : _role;
            set => _role = string.IsNullOrWhiteSpace(value) ? "User" : value;
        }
    }
}
