using System;
using System.Net;
using aRefactor.Domain.Dto;
using aRefactor.Extension;
using ResponseMessage = aRefactor.Domain.Exception.Response;

namespace aRefactor.Domain.Type;

public class Response
{
    public bool Success { get; set; }
    public ResponseMessage MessageKey { get; set; } = ResponseMessage.Success;
    public string Message { get; set; } = ResponseMessage.Success.GetDescriptionOfEnum();
    public string? ErrorCode { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static Response SuccessResponse(CreateResponsePattern result,
        ResponseMessage messageKey = ResponseMessage.Success, string? messageOverride = null)
    {
        return new Response
        {
            Success = true,
            MessageKey = messageKey,
            Message = messageOverride ?? messageKey.GetDescriptionOfEnum(),
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    public static Response ErrorResponse(ResponseMessage messageKey, string? messageOverride = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string? errorCode = null)
    {
        return new Response
        {
            Success = false,
            MessageKey = messageKey,
            Message = messageOverride ?? messageKey.GetDescriptionOfEnum(),
            StatusCode = (int)statusCode,
            ErrorCode = errorCode ?? statusCode.ToString()
        };
    }
}

public class Response<T> : Response
{
    public T? Data { get; set; }

    public static Response<T> SuccessResponse(T data, ResponseMessage messageKey = ResponseMessage.Success, string? messageOverride = null)
    {
        return new Response<T>
        {
            Success = true,
            MessageKey = messageKey,
            Message = messageOverride ?? messageKey.GetDescriptionOfEnum(),
            Data = data,
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    public static new Response<T> ErrorResponse(ResponseMessage messageKey, string? messageOverride = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string? errorCode = null)
    {
        return new Response<T>
        {
            Success = false,
            MessageKey = messageKey,
            Message = messageOverride ?? messageKey.GetDescriptionOfEnum(),
            StatusCode = (int)statusCode,
            ErrorCode = errorCode ?? statusCode.ToString()
        };
    }
}