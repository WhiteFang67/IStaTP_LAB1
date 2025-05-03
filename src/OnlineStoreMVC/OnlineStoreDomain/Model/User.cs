using Microsoft.AspNetCore.Identity;

namespace OnlineStoreDomain.Models
{
    public class User : IdentityUser
    {
        public DateOnly? BirthDate { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }
}