using Microsoft.AspNetCore.Identity;

namespace Project.Entities;

public class AppUser : IdentityUser
{
    public bool Active { get; set; } = true;
}
