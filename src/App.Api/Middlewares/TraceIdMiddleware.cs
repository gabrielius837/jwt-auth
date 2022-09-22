namespace App.Api;

public class TraceIdMiddleware
{
    public const string TRACE_ID_HEADER = "X-Trace-Id";
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var existingHeader = context.Request.Headers[TRACE_ID_HEADER].FirstOrDefault();
        if (existingHeader is null)
        {
            var id = Guid.NewGuid().ToString();
            context.TraceIdentifier = id;
            context.Response.Headers.Add(TRACE_ID_HEADER, id);
        }
        await _next(context);
    }
}

public static class TraceIdMiddlewareExtensions
{
    public static IApplicationBuilder UseTraceId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TraceIdMiddleware>();
    }
}