namespace aRefactor.Domain.Model;


// 5. Bảng chứa 1 ví dụ Refactor (Trước/Sau)
public class RefactorExample
{
    public Guid Id { get; set; }
    public string Title { get; set; } // "Ví dụ: Refactor khối switch-case sang Strategy"
    public string Problem { get; set; } // Mô tả code "xấu"

    // Foreign Key
    public Guid PatternId { get; set; }

    // Navigation Properties
    public virtual Pattern Pattern { get; set; }
    
    // 1 ví dụ Refactor có nhiều Snippets (thường là 2: Trước/Sau)
    public virtual ICollection<RefactorSnippet> Snippets { get; set; } = new HashSet<RefactorSnippet>();
}