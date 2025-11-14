using aRefactor.Domain.Exception;
using aRefactor.Extension;

namespace aRefactor.Domain.Dto;

public class GetRequestPattern
{
    public string Slug { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrEmpty(Slug))
        {
            throw new ProjectException(Response.SlugCannotBeEmpty.GetDescriptionOfEnum());
        }
    }
}
public class GetResponsePattern
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}