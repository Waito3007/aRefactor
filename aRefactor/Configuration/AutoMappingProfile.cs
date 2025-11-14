using AutoMapper;
using aRefactor.Domain.Dto;
using aRefactor.Domain.Model;

namespace aRefactor.Configuration;

public class AutoMappingProfile : Profile
{
    public AutoMappingProfile()
    {
        CreateMap<CreateRequestPattern, Pattern>()
            .ForMember(destination => destination.Id, options => options.Ignore());

        CreateMap<Pattern, CreateRequestPattern>();
    }
}
