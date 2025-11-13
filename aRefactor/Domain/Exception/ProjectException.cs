using System.Net;

namespace aRefactor.Domain.Exception;

public class ProjectException : System.Exception
{
    public HttpStatusCode StatusCode { get; set; }
    public string ErrorCode { get; set; }
    public Response MessageKey { get; }
    
    public ProjectException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        string errorCode = null, Response messageKey = Response.Success) 
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode ?? statusCode.ToString();
        MessageKey = messageKey;
    }
    
    public ProjectException(string message, System.Exception innerException, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest, string errorCode = null, Response messageKey = Response.Success) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode ?? statusCode.ToString();
        MessageKey = messageKey;
    }
}
