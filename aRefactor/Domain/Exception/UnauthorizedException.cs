using System.Net;
using aRefactor.Extension;

namespace aRefactor.Domain.Exception;

public class UnauthorizedException : ProjectException
{
    public UnauthorizedException(string message = null)
        : base(
            message ?? Response.Unauthorized.GetDescriptionOfEnum(),
            HttpStatusCode.Unauthorized,
            "UNAUTHORIZED",
            Response.Unauthorized)
    {
    }
}
