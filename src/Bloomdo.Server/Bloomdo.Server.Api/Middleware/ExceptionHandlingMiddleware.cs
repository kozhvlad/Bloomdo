using System.Net;
using Bloomdo.Server.Api.Extensions;
using FluentValidation;

namespace Bloomdo.Server.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (ValidationException e)
        {
            logger.LogWarning(
                $"A validation exception was thrown with errors: [{string.Join(", ", e.Errors.Select(failure => failure.ErrorMessage))}]");

            await HttpContextExtensions.SetResponse(e, context, HttpStatusCode.BadRequest).ConfigureAwait(false);
        }
        catch (ArgumentException e)
        {
            logger.LogWarning($"{nameof(ArgumentException)} was thrown with error: {e.Message}");

            await HttpContextExtensions.SetResponse(e, context, HttpStatusCode.BadRequest).ConfigureAwait(false);
        }
        catch (Exception mainException)
        {
            var message = (mainException.InnerException != null ? mainException.Message + " -->" + mainException.InnerException.Message : mainException.Message);
            logger.LogError(mainException, $"Exception: {message}");

            if (context.Response.HasStarted)
            {
                logger.LogWarning("The response has already started, the error handler will not be executed.");
                throw;
            }

            try
            {
                await HttpContextExtensions.SetResponse(mainException, context, HttpStatusCode.InternalServerError).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(0, exception, $"The main exception handler threw while trying to report and format an error {mainException}.");
                throw;
            }
        }
    }
}
