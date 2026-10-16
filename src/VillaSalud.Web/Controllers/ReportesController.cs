using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Models;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

[Authorize(Roles = AdminORecepcion)]
public class ReportesController : ControladorBase
{
    public ReportesController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index([FromQuery] FiltroCitasDto filtro)
    {
        filtro.Desde ??= new DateOnly(Hoy.Year, Hoy.Month, 1);
        filtro.Hasta ??= filtro.Desde.Value.AddMonths(1).AddDays(-1);
        var vm = new ReportesVm { Filtro = filtro, Medicos = await Api.GetAsync<List<MedicoDto>>("api/v1/medicos") };
        try
        {
            vm.Reporte = await Api.GetAsync<ReporteCitasDto>(RutaCitas("api/v1/reportes/citas", filtro));
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            ModelState.AddModelError(string.Empty, ex.Message);     // por ejemplo, periodo mayor a 12 meses
        }
        return View(vm);
    }

    /// <summary>Descarga el reporte en PDF, Excel o CSV. El archivo lo genera la API.</summary>
    public async Task<IActionResult> Exportar([FromQuery] FiltroCitasDto filtro, string formato = "xlsx")
    {
        try
        {
            var (contenido, mime, nombre) = await Api.DescargarAsync(RutaCitas("api/v1/reportes/citas/exportar", filtro, ("formato", formato)));
            return File(contenido, mime, nombre);
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            Aviso(ex.Message);
            return RedirectToAction(nameof(Index), new { desde = filtro.Desde.HasValue ? Fecha(filtro.Desde.Value) : null, hasta = filtro.Hasta.HasValue ? Fecha(filtro.Hasta.Value) : null });
        }
    }
}
