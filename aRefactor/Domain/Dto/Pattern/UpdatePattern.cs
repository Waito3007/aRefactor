using System;
using aRefactor.Domain.Exception;
using aRefactor.Extension;

namespace aRefactor.Domain.Dto;


public class UpdateRequestPattern
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ProjectException(Response.NameCannotBeEmpty.GetDescriptionOfEnum());
        }

        if (string.IsNullOrWhiteSpace(Slug))
        {
            throw new ProjectException(Response.SlugCannotBeEmpty.GetDescriptionOfEnum());
        }
    }
}

public class UpdateResponsePattern
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
}