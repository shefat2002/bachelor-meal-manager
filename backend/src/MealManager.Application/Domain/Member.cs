using Microsoft.AspNetCore.Identity;

namespace MealManager.Application.Domain;

/// <summary>Identity user. UserName and PhoneNumber both hold the normalized phone number.</summary>
public class Member : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<MemberMess> Messes { get; set; } = new List<MemberMess>();
}
