using System.ComponentModel;

namespace aRefactor.Domain.Enum;

public enum RefactorSnippetType
{
    [Description("Trước khi Refactor Code Snippet")]
    Before,
    [Description("Sau khi Refactor Code Snippet")]
    After
}