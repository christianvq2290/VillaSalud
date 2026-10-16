using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Models;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

public class InicioController : ControladorBase
{
    public InicioController(ApiCliente api) : base(api) { }

    /// <summary>Tablero: accesos a los módulos según el rol e indicadores del mes para administración y recepción.</summary>
    public async Task<IActionResult> Index()
    {
        var vm = new TableroVm { VerIndicadores = !EsMedico };
        if (vm.VerIndicadores)
            vm.Indicadores = await Api.GetAsync<IndicadoresDto>("api/v1/dashboard/indicadores");
        return View(vm);
    }

    /// <summary>Página pública con la información de la clínica y el asistente virtual.</summary>
    [AllowAnonymous]
    public IActionResult Portal() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["Mensaje"] = "Ocurrió un error inesperado. Intente nuevamente.";
        return View();
    }
}
