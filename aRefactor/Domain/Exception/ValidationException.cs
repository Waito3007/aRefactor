using System.Collections.Generic;
using System.Net;
using aRefactor.Extension;

namespace aRefactor.Domain.Exception;

public class ValidationException : ProjectException
{
    public Dictionary<string, string[]> Errors { get; set; }

    public ValidationException(string message = null, Dictionary<string, string[]> errors = null)
        : base(
            message ?? Response.ValidationError.GetDescriptionOfEnum(),
            HttpStatusCode.BadRequest,
            "VALIDATION_ERROR",
            Response.ValidationError)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : this(null, errors)
    {
    }
}
