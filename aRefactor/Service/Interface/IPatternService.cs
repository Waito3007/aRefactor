using aRefactor.Domain.Dto;

namespace aRefactor.Service.Interface;

public interface IPatternService
{
    Task<CreateResponsePattern> CreatePatternAsync(CreateRequestPattern requestPattern);
    Task<UpdateResponsePattern> UpdatePatternAsync(UpdateRequestPattern requestPattern);
    Task<GetResponsePattern> GetPatternAsync(GetRequestPattern requestPattern); 
}
