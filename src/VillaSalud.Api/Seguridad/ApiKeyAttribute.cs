using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VillaSalud.Api.Seguridad;

/// <summary>
/// Protege los servicios públicos que consume el asistente virtual: exige la cabecera X-Api-Key
/// con el valor configurado en Asistente:ApiKey. Si no hay clave configurada, el canal queda cerrado.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string Cabecera = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuracion = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var esperada = configuracion["Asistente:ApiKey"];
        var recibida = context.HttpContext.Request.Headers[Cabecera].FirstOrDefault();

        var valida = !string.IsNullOrWhiteSpace(esperada) && !string.IsNullOrEmpty(recibida) &&
                     CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperada), Encoding.UTF8.GetBytes(recibida));
        if (!valida)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "No autorizado",
                Detail = "Clave de API ausente o no válida."
            }) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }
}
