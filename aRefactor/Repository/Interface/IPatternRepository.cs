using aRefactor.Domain.Dto;
using aRefactor.Domain.Model;

namespace aRefactor.Repository.Interface;

public interface IPatternRepository
{
    void Add(Pattern request);
    void Update(Pattern request);
    void Delete(Pattern request);
    Task<Pattern?> GetBySlug(string Slug);
}