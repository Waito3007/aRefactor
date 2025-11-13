using System.Net;
using aRefactor.Extension;

namespace aRefactor.Domain.Exception;

public class NotFoundException : ProjectException
{
    public NotFoundException(string message = null)
        : base(
            message ?? Response.NotFound.GetDescriptionOfEnum(),
            HttpStatusCode.NotFound,
            "NOT_FOUND",
            Response.NotFound)
    {
    }

    public NotFoundException(string entityName, object key)
        : base(
            $"{entityName} voi ID '{key}' khong ton tai.",
            HttpStatusCode.NotFound,
            "NOT_FOUND",
            Response.NotFound)
    {
    }
}
