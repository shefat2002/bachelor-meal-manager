namespace MealManager.Application.Domain;

public class Mess
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<MemberMess> Members { get; set; } = new List<MemberMess>();
}
