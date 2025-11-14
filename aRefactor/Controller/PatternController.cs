using System;
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
        var result = await _patternService.CreatePatternAsync(request);
        var response = Response<CreateResponsePattern>.SuccessResponse(result);
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{patternId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid patternId, [FromBody] UpdateRequestPattern request)
    {
        request.Id = patternId;
        var result = await _patternService.UpdatePatternAsync(request);
        var response = Response<UpdateResponsePattern>.SuccessResponse(result);
        response.StatusCode = StatusCodes.Status200OK;
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetPatternAsync(string slug)
    {
        var request = new GetRequestPattern { Slug = slug };
        var result = await _patternService.GetPatternAsync(request);
        var response = Response<GetResponsePattern>.SuccessResponse(result);
        response.StatusCode = StatusCodes.Status200OK;
        return StatusCode(response.StatusCode, response);
    }
}
