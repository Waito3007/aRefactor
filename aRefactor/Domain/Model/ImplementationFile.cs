namespace aRefactor.Domain.Model;


// 4. Bảng chứa 1 file code (để build cây thư mục)
public class ImplementationFile
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } // Mấu chốt: "/Core/Interfaces/IStrategy.cs"
    public string CodeBlock { get; set; } // Nội dung code
    public string Language { get; set; } // "csharp", "json", "sql"
    public string? Notes { get; set; } // Ghi chú riêng cho file này (nullable)

    // Foreign Key
    public Guid ImplementationId { get; set; }

    // Navigation Property
    public virtual Implementation Implementation { get; set; }
}