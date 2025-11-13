namespace aRefactor.Domain.Model;


// 1. Bảng "Cha" để nhóm
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } // "Creational", "Structural", "Behavioral"
    public string Slug { get; set; }

    // Navigation Property: 1 Category có nhiều Patterns
    public virtual ICollection<Pattern> Patterns { get; set; } = new HashSet<Pattern>();
}