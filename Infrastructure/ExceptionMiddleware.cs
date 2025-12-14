using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Infrastructure
{
    public class ExceptionMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message} {ex.StackTrace}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            if (!Guid.TryParse(context.Request.Headers["X-Correlation-ID"], out var correlationId))
            {
                correlationId = Guid.NewGuid();
            }
            var response = new BaseResponse(
                message: exception.Message,
                status: BaseResponseStatus.Error,
                correlationId: correlationId
            );
            await context.Response.WriteAsync(response.ToString());
        }
    }
}
