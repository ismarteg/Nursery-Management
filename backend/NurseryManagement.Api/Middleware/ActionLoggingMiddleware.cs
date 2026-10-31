using System.Diagnostics;
using System.Security.Claims;
using NurseryManagement.Api.Domain.Entities;
using NurseryManagement.Api.Services.Logging;

namespace NurseryManagement.Api.Middleware;

public class ActionLoggingMiddleware(RequestDelegate next, ILogger<ActionLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!ShouldLog(context, path))
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        Exception? caught = null;
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            caught = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            try
            {
                await WriteLogAsync(context, scopeFactory, path, sw.ElapsedMilliseconds, caught);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write application action log");
            }
        }
    }

    private static bool ShouldLog(HttpContext context, string path)
    {
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return false;
        }

        if (path.StartsWith("/api/logs", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static async Task WriteLogAsync(
        HttpContext context,
        IServiceScopeFactory scopeFactory,
        string path,
        long elapsedMs,
        Exception? caught)
    {
        var status = caught is null ? context.Response.StatusCode : StatusCodes.Status500InternalServerError;
        var logType = status switch
        {
            >= 500 => AppLogType.Error,
            >= 400 => AppLogType.Warning,
            _ => AppLogType.Success
        };

        if (HttpMethods.IsGet(context.Request.Method) && status < 400)
        {
            logType = AppLogType.Info;
        }

        var module = ResolveModule(path);
        var action = context.Request.Method.ToUpperInvariant();
        var succeeded = caught is null && status < 400;
        var message = caught is null
            ? $"{action} {path} -> {status} ({elapsedMs} ms)"
            : $"{action} {path} failed: {caught.Message}";

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdValue, out var id) ? id : null;
        var userName = context.User.Identity?.Name
            ?? context.User.FindFirstValue(ClaimTypes.Name);

        using var scope = scopeFactory.CreateScope();
        var logs = scope.ServiceProvider.GetRequiredService<IAppLogService>();
        await logs.WriteAsync(
            logType,
            module,
            action,
            message,
            succeeded,
            status,
            details: caught?.ToString(),
            userId,
            userName,
            context.Connection.RemoteIpAddress?.ToString());
    }

    private static string ResolveModule(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : "api";
    }
}
