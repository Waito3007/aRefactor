namespace aRefactor.Domain.Model;
using System;
using System.Collections.Generic;


// 3. Bảng chứa 1 ví dụ triển khai (cho cây thư mục)
public class Implementation
{
    public Guid Id { get; set; }
    public string Title { get; set; } // "Ví dụ: Áp dụng Strategy cho Payment"
    public string Description { get; set; } // Giải thích ví dụ này

    // Foreign Key
    public Guid PatternId { get; set; }

    // Navigation Properties
    public virtual Pattern Pattern { get; set; }
    
    // 1 Implementation có nhiều Files
    public virtual ICollection<ImplementationFile> Files { get; set; } = new HashSet<ImplementationFile>();
}




