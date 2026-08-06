using System.Text.Json;
using ERMS.Application.Common.Exceptions;

namespace ERMS.Api.Middleware;

/// <summary>
/// Tüm controller'lardan fırlatılan AppException türevlerini (ve beklenmeyen hataları)
/// Bölüm 5.6'daki standart hata modeline çevirir: { "code": "...", "message": "..." }.
/// Controller'larda try-catch yazılmasını gereksiz kılar.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationAppException ex)
        {
            await WriteResponseAsync(context, ex.StatusCode, new
            {
                code = ex.Code,
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (AppException ex)
        {
            await WriteResponseAsync(context, ex.StatusCode, new
            {
                code = ex.Code,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen sunucu hatası: {Path}", context.Request.Path);

            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError, new
            {
                code = "INTERNAL_ERROR",
                message = "Beklenmeyen bir sunucu hatası oluştu."
            });
        }
    }

    private static Task WriteResponseAsync(HttpContext context, int statusCode, object body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
