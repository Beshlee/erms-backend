using System.Text.Json;
using ERMS.Application.Common.Exceptions;

namespace ERMS.Api.Middleware;

/// <summary>
/// Tüm controller'lardan fırlatılan AppException türevlerini (ve beklenmeyen hataları)
/// Bölüm 5.6'daki standart hata modeline çevirir: { "code": "...", "message": "..." }.
/// Controller'larda try-catch yazılmasını gereksiz kılar.
///
/// ASP.NET Core "middleware" nedir: her gelen HTTP isteği, Program.cs'te sırayla eklenen
/// (app.UseMiddleware, app.UseCors, app.UseAuthentication, ...) bir zincirden geçer; her
/// halka isteği bir sonrakine (<see cref="_next"/>) devreder, sonra yanıt geri dönerken de
/// aynı sırayla (tersten) tekrar çalışır. Bu sınıf zincirin EN BAŞINA konur (Program.cs'te
/// ilk middleware) ki zincirdeki HERHANGİ bir yerde (controller, sonraki middleware'ler)
/// fırlatılan bir istisnayı yakalayabilsin — try/catch, `await _next(context)` çağrısını sarar.
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
            // Zincirdeki bir sonraki adıma geç — controller'a kadar giden tüm ara katmanlar
            // ve controller action'ının kendisi burada, bu satırın "içinde" çalışır.
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
