using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Api.Configuracion;

/// <summary>Convierte las excepciones en respuestas ProblemDetails (RFC 7807) con el código HTTP que corresponde.</summary>
public class ManejadorErrores : IExceptionHandler
{
    private readonly ILogger<ManejadorErrores> _logger;

    public ManejadorErrores(ILogger<ManejadorErrores> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (estado, titulo) = exception switch
        {
            ReglaNegocioException => (StatusCodes.Status400BadRequest, "Regla de negocio"),
            CredencialesInvalidasException => (StatusCodes.Status401Unauthorized, "No autenticado"),
            AccesoDenegadoException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            NoEncontradoException => (StatusCodes.Status404NotFound, "No encontrado"),
            ConflictoException => (StatusCodes.Status409Conflict, "Conflicto"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno")
        };

        if (estado == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}", httpContext.Request.Method, httpContext.Request.Path);

        var problema = new ProblemDetails
        {
            Status = estado,
            Title = titulo,
            // Los errores internos no exponen detalles técnicos al cliente
            Detail = estado == StatusCodes.Status500InternalServerError ? "Ocurrió un error inesperado. Intente nuevamente." : exception.Message,
            Instance = httpContext.Request.Path
        };
        httpContext.Response.StatusCode = estado;
        await httpContext.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}
