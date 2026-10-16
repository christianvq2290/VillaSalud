using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace VillaSalud.Web.Servicios;

/// <summary>
/// Traduce los errores de la API que el controlador no manejó: 401 → volver a ingresar,
/// 403 → página de acceso denegado, el resto → página de error con el mensaje de la API.
/// </summary>
public class FiltroErroresApi : IAsyncExceptionFilter
{
    private readonly IModelMetadataProvider _metadatos;

    public FiltroErroresApi(IModelMetadataProvider metadatos) => _metadatos = metadatos;

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiException error) return;

        if (error.EsNoAutenticado)
        {
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Result = new RedirectToActionResult("Ingresar", "Cuenta", new { expirada = true });
        }
        else if (error.EsAccesoDenegado)
        {
            context.Result = new RedirectToActionResult("AccesoDenegado", "Cuenta", null);
        }
        else
        {
            var datos = new ViewDataDictionary(_metadatos, context.ModelState) { ["Mensaje"] = error.Message };
            context.Result = new ViewResult { ViewName = "Error", ViewData = datos, StatusCode = (int)error.Estado };
        }
        context.ExceptionHandled = true;
    }
}

/// <summary>Mientras el usuario tenga una clave temporal, solo puede ir a cambiarla o salir.</summary>
public class FiltroCambioClave : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var usuario = context.HttpContext.User;
        var controlador = context.RouteData.Values["controller"]?.ToString();
        if (usuario.Identity?.IsAuthenticated == true && usuario.FindFirst(ServicioSesion.ClaimCambiarClave)?.Value == "1" && controlador != "Cuenta")
        {
            context.Result = new RedirectToActionResult("CambiarClave", "Cuenta", null);
            return Task.CompletedTask;
        }
        return next();
    }
}

public static class ExtensionesControlador
{
    /// <summary>Pasa al formulario los errores devueltos por la API (por campo cuando la API los identifica).</summary>
    public static void AgregarErrores(this ModelStateDictionary estado, ApiException error)
    {
        if (error.Errores.Count == 0)
        {
            estado.AddModelError(string.Empty, error.Message);
            return;
        }
        foreach (var (campo, mensajes) in error.Errores)
            foreach (var mensaje in mensajes)
                estado.AddModelError(campo.StartsWith('$') ? string.Empty : campo, mensaje);
    }
}
