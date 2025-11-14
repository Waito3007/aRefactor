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
        CreateMap<Pattern, CreateResponsePattern>();

        CreateMap<UpdateRequestPattern, Pattern>()
            .ForMember(destination => destination.Id, options => options.Ignore());
        CreateMap<Pattern, UpdateResponsePattern>();
        CreateMap<Pattern, UpdateRequestPattern>();

        CreateMap<Pattern, GetResponsePattern>()
            .ForMember(destination => destination.CategoryName, options => options.MapFrom(source => source.Category != null ? source.Category.Name : string.Empty));
    }
}
