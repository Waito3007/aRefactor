using aRefactor.Domain.Dto;
using aRefactor.Domain.Type;
using aRefactor.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace aRefactor.Controller;

[ApiController]
[Route("api/[controller]")]
public class PatternController : ControllerBase
{
    private readonly IPatternService _patternService;

    public PatternController(IPatternService patternService)
    {
        _patternService = patternService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRequestPattern request)
    {
        var dto = await _patternService.CreatePatternAsync(request);
        var response = Response<CreateRequestPattern>.SuccessResponse(dto);
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(response.StatusCode, response);
    }
}