using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

public class CuentaController : Controller
{
    private readonly ApiCliente _api;
    private readonly ServicioSesion _sesion;

    public CuentaController(ApiCliente api, ServicioSesion sesion)
    {
        _api = api;
        _sesion = sesion;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Ingresar(string? returnUrl = null, bool expirada = false)
    {
        if (User.Identity?.IsAuthenticated == true && !expirada) return RedirectToAction("Index", "Inicio");
        ViewData["ReturnUrl"] = returnUrl;
        if (expirada) ViewData["Aviso"] = "La sesión expiró. Ingrese nuevamente.";
        return View(new LoginDto());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Ingresar(LoginDto dto, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(dto);

        try
        {
            var sesion = await _api.PostAsync<SesionDto>("api/v1/auth/login", dto);
            await _sesion.IniciarAsync(HttpContext, sesion);
            if (sesion.DebeCambiarClave) return RedirectToAction(nameof(CambiarClave));
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Inicio");
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            dto.Password = "";
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        try
        {
            await _api.PostAsync("api/v1/auth/logout");
        }
        catch (ApiException)
        {
            // aunque la API no responda, la sesión local se cierra igual
        }
        await _sesion.CerrarAsync(HttpContext);
        return RedirectToAction(nameof(Ingresar));
    }

    [HttpGet]
    public IActionResult CambiarClave()
    {
        ViewData["Obligatorio"] = User.FindFirstValue(ServicioSesion.ClaimCambiarClave) == "1";
        return View(new CambiarClaveDto());
    }

    [HttpPost]
    public async Task<IActionResult> CambiarClave(CambiarClaveDto dto)
    {
        ViewData["Obligatorio"] = User.FindFirstValue(ServicioSesion.ClaimCambiarClave) == "1";
        if (!ModelState.IsValid) return View(dto);

        try
        {
            await _api.PostAsync("api/v1/auth/cambiar-clave", dto);
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado)
        {
            ModelState.AgregarErrores(ex);
            return View(new CambiarClaveDto());
        }

        // Al cambiar la clave la API revoca las sesiones abiertas: se pide ingresar con la clave nueva
        await _sesion.CerrarAsync(HttpContext);
        TempData["Exito"] = "Contraseña actualizada. Ingrese con su nueva contraseña.";
        return RedirectToAction(nameof(Ingresar));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}
