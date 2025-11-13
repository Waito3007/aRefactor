using aRefactor.Domain.Exception;
using aRefactor.Extension;

namespace aRefactor.Domain.Dto;

public class CreateRequestPattern
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Summary { get; set; }
    public string Problem { get; set; }
    public string Solution { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ProjectException(Response.NameCannotBeEmpty.GetDescriptionOfEnum());
        }

        if (string.IsNullOrWhiteSpace(Slug))
        {
            throw new ArgumentException("Pattern slug cannot be empty.");
        }
    }
}

public class CreateResponsePattern
{

}
