using System.Net;
using System.Text.Json;
using ValidationException = FluentValidation.ValidationException;

namespace Bloomdo.Server.Api.Extensions;

public static class HttpContextExtensions
{
    private static readonly Func<HttpStatusCode, Exception, string> _exceptionResponseMessage = (statusCode, exception) =>
    {
        switch (statusCode)
        {
            case HttpStatusCode.InternalServerError:
                return $"Internal server error. [{(exception.InnerException != null ? exception.Message + " -->" + exception.InnerException.Message : exception.Message)}]";
            default:
                return exception.Message;
        }
    };

    private static string ResponseMessage(this Exception exception, HttpStatusCode statusCode)
    {
        if (exception is ValidationException validationException)
        {
            var validationErrorResponse = new
            {
                statusCode,
                validationException.Message,
                errors = validationException.Errors.Select(e => e.ErrorMessage)
            };

            return JsonSerializer.Serialize(validationErrorResponse);
        }

        var errorResponse = new
        {
            statusCode,
            message = _exceptionResponseMessage(statusCode, exception)
        };

        return JsonSerializer.Serialize(errorResponse);
    }

    public static async Task SetResponse(Exception exception, HttpContext context, HttpStatusCode statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(exception.ResponseMessage(statusCode)).ConfigureAwait(false);
    }
}