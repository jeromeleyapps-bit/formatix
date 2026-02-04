using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace FormationManager.Infrastructure.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Exception non gérée : {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = exception switch
                {
                    ArgumentNullException => (int)HttpStatusCode.BadRequest,
                    ArgumentException => (int)HttpStatusCode.BadRequest,
                    UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                    KeyNotFoundException => (int)HttpStatusCode.NotFound,
                    InvalidOperationException => (int)HttpStatusCode.Conflict,
                    BusinessException businessEx => (int)HttpStatusCode.BadRequest,
                    SyncException syncEx => (int)HttpStatusCode.ServiceUnavailable,
                    _ => (int)HttpStatusCode.InternalServerError
                },
                Title = exception.GetType().Name,
                Detail = _environment.IsDevelopment() 
                    ? exception.ToString() 
                    : exception.Message,
                Instance = httpContext.Request.Path
            };

            // Ajout d'informations supplémentaires pour les exceptions métier
            if (exception is BusinessException businessException)
            {
                problemDetails.Extensions.Add("errorCode", businessException.ErrorCode);
            }

            if (exception is SyncException syncException)
            {
                problemDetails.Extensions.Add("siteId", syncException.SiteId);
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(problemDetails, options),
                cancellationToken);

            return true;
        }
    }

    // Exceptions métier personnalisées
    public class BusinessException : Exception
    {
        public string ErrorCode { get; }
        
        public BusinessException(string errorCode, string message) 
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public BusinessException(string errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    public class SyncException : Exception
    {
        public string SiteId { get; }
        
        public SyncException(string siteId, string message, Exception? innerException = null) 
            : base(message, innerException)
        {
            SiteId = siteId;
        }
    }

    public class OCRException : BusinessException
    {
        public OCRException(string message) 
            : base("OCR_ERROR", message)
        {
        }

        public OCRException(string message, Exception innerException) 
            : base("OCR_ERROR", message, innerException)
        {
        }
    }

    public class AIException : BusinessException
    {
        public AIException(string message) 
            : base("AI_ERROR", message)
        {
        }

        public AIException(string message, Exception innerException) 
            : base("AI_ERROR", message, innerException)
        {
        }
    }
}