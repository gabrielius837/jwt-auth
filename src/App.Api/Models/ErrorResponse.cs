namespace App.Api.Models;

public record ErrorResponse(string TraceId, int StatusCode, string Message);