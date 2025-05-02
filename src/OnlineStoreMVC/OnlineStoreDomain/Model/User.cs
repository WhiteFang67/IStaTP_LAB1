using Microsoft.AspNetCore.Identity;

namespace OnlineStoreDomain.Models
{
    public class User : IdentityUser
    {
        public int Year { get; set; } 
    }
}