using aRefactor.Domain.Enum;
namespace aRefactor.Domain.Model;


// 6. Bảng chứa đoạn code "Trước" hoặc "Sau"
public class RefactorSnippet
{
    public Guid Id { get; set; }
    
    public RefactorSnippetType Type { get; set; } // "Before" hoặc "After"
    
    public string CodeBlock { get; set; }
    public string Language { get; set; }
    public string? Notes { get; set; }

    // Foreign Key
    public Guid RefactorExampleId { get; set; }

    // Navigation Property
    public virtual RefactorExample RefactorExample { get; set; }
}