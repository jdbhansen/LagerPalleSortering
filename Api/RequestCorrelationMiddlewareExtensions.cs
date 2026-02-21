using System.Collections.Generic;

namespace LagerPalleSortering.Api;

public static class RequestCorrelationMiddlewareExtensions
{
    public const string CorrelationHeaderName = "X-Correlation-ID";

    public static IApplicationBuilder UseRequestCorrelation(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("RequestCorrelation");

            var requestCorrelationId = context.Request.Headers[CorrelationHeaderName].ToString();
            var correlationId = string.IsNullOrWhiteSpace(requestCorrelationId)
                ? context.TraceIdentifier
                : requestCorrelationId.Trim();

            context.Response.Headers[CorrelationHeaderName] = correlationId;

            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await next();
                logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode);
            }
        });
    }
}
