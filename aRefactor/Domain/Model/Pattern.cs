namespace aRefactor.Domain.Model;


// 2. Bảng chứa thông tin về 1 Pattern
public class Pattern
{
    public Guid Id { get; set; }
    public string Name { get; set; } // "Strategy", "Repository"
    public string Slug { get; set; }
    public string Summary { get; set; } // Mô tả tóm tắt
    public string Problem { get; set; } // Vấn đề pattern này giải quyết
    public string Solution { get; set; } // Cách nó giải quyết

    // Foreign Key
    public Guid CategoryId { get; set; }
    
    // Navigation Properties
    public virtual Category Category { get; set; }
    public virtual ICollection<Implementation> Implementations { get; set; } = new HashSet<Implementation>();
    public virtual ICollection<RefactorExample> RefactorExamples { get; set; } = new HashSet<RefactorExample>();
}
