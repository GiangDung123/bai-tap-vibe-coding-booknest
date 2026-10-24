using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace BookNest.Api.Infrastructure;
public class AppException(int status, string message) : Exception(message) { public int Status { get; } = status; }
public class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested) { }
        catch (Exception ex)
        {
            var sql = ex as MySqlException ?? ex.InnerException as MySqlException;
            var (status, detail) = ex switch
            {
                AppException a => (a.Status, a.Message),
                DbUpdateConcurrencyException => (409, "Dữ liệu đã thay đổi. Vui lòng tải lại trước khi lưu."),
                _ when sql?.Number == 1062 => (409, "Email, slug, SKU hoặc dữ liệu duy nhất đã tồn tại."),
                _ when sql?.Number is 1205 or 1213 => (409, "Có giao dịch đồng thời. Vui lòng thử lại với cùng mã yêu cầu."),
                _ => (500, "Không thể xử lý yêu cầu. Vui lòng thử lại.")
            };
            if (status == 500) logger.LogError("Request failed: {Type}; trace {Trace}", ex.GetType().Name, ctx.TraceIdentifier);
            if (!ctx.Response.HasStarted) await Results.Problem(statusCode: status, title: detail, extensions: new Dictionary<string, object?> { ["traceId"] = ctx.TraceIdentifier }).ExecuteAsync(ctx);
        }
    }
}