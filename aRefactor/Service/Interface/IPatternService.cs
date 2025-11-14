using aRefactor.Domain.Dto;

namespace aRefactor.Service.Interface;

public interface IPatternService
{
    Task<CreateRequestPattern> CreatePatternAsync(CreateRequestPattern requestPattern);
}