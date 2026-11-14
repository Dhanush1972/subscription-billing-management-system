using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Domain.Exceptions;

namespace SubscriptionBilling.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request to {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                notFound.Message
            ),
            BusinessRuleValidationException businessRule => (
                HttpStatusCode.BadRequest,
                "Business Rule Violation",
                businessRule.Message
            ),
            FluentValidation.ValidationException validationEx => (
                HttpStatusCode.UnprocessableEntity,
                "Validation Error",
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))
            ),
            UnauthorizedAccessException unauthorized => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                unauthorized.Message
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please contact system administrator."
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
