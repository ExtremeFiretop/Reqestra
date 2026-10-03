using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Ombi
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context /* other scoped dependencies */)
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The client disconnected or navigated away while ASP.NET/EF Core was
                // still processing the request. RequestAborted is expected to cancel
                // in-flight database work in this case; there is no client left to send
                // an error response to, so treat it as a normal abandoned request.
                return;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var loggerFact = context.RequestServices.GetService<ILoggerFactory>();
            var logger = loggerFact.CreateLogger<ErrorHandlingMiddleware>();
            logger.LogError(exception, "Something bad happened, ErrorMiddleware caught this");

            // Once response headers/body have started it is too late to replace the
            // response with our JSON error payload. Avoid causing a second exception
            // while trying to report the original one.
            if (context.Response.HasStarted)
            {
                logger.LogWarning("The response had already started, so ErrorHandlingMiddleware could not write an error response.");
                return Task.CompletedTask;
            }

            var code = HttpStatusCode.InternalServerError; // 500 if unexpected

            //if (exception is NotFoundException) code = HttpStatusCode.NotFound;
            if (exception is UnauthorizedAccessException) code = HttpStatusCode.Unauthorized;
            if (exception is OperationCanceledException) code = HttpStatusCode.NoContent;

            context.Response.StatusCode = (int)code;

            // HTTP 204 responses are not allowed to contain a response body. The old
            // middleware set 204 and then called WriteAsync, which caused Kestrel to
            // throw "Writing to the response body is invalid for responses with status
            // code 204" and obscured the original cancellation.
            if (code == HttpStatusCode.NoContent)
            {
                return Task.CompletedTask;
            }

            string result;
            if (exception.InnerException != null)
            {
                result = JsonConvert.SerializeObject(new { error = exception.InnerException.Message });
            }
            else
            {
                result = JsonConvert.SerializeObject(new { error = exception.Message });
            }
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(result);
        }
    }
}