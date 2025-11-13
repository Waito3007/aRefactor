using System.Net;
using System.Text.Json;
using aRefactor.Domain.Exception;
using ResponseModel = aRefactor.Domain.Type.Response;
using ValidationErrorResponse = aRefactor.Domain.Type.Response<System.Collections.Generic.Dictionary<string, string[]>>;

namespace aRefactor.Configuration;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        object response;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ValidationErrorResponse
                {
                    Success = false,
                    MessageKey = validationException.MessageKey,
                    Message = validationException.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    ErrorCode = "VALIDATION_ERROR",
                    Data = validationException.Errors,
                    Timestamp = DateTime.UtcNow
                };
                _logger.LogWarning(exception, "Validation exception occurred: {Message}", validationException.Message);
                break;

            case ProjectException projectException:
                context.Response.StatusCode = (int)projectException.StatusCode;
                response = ResponseModel.ErrorResponse(
                    projectException.MessageKey,
                    projectException.Message,
                    projectException.StatusCode,
                    projectException.ErrorCode
                );
                _logger.LogWarning(exception, "Project exception occurred: {Message}", projectException.Message);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = ResponseModel.ErrorResponse(
                    Response.InternalServerError,
                    null,
                    HttpStatusCode.InternalServerError,
                    "INTERNAL_SERVER_ERROR"
                );
                _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
