using Infrastructure.Entities;
using Infrastructure.Utils;

namespace AccountService.Entities
{
    public class Account: TenancyEntity
    {
        public Account()
        {
            
        }
        public Account(string email, string password, string? name, string? avatar)
        {
            Email = email;
            Password = PasswordHasher.HashPassword(password);
            Name = name;
            Avatar = avatar;
        }

        public string Email { get; set; }
        public string Password { get; set; }
        public string? Name { get; set; }
        public string? Avatar { get; set; }
        public bool IsAdmin { get; set; } = false;

    }
}
