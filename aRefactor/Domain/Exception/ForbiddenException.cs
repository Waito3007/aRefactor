using System.Net;
using aRefactor.Extension;

namespace aRefactor.Domain.Exception;

public class ForbiddenException : ProjectException
{
    public ForbiddenException(string message = null)
        : base(
            message ?? Response.Forbidden.GetDescriptionOfEnum(),
            HttpStatusCode.Forbidden,
            "FORBIDDEN",
            Response.Forbidden)
    {
    }
}
