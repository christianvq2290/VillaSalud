using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

[Authorize(Roles = Admin)]
public class SedesController : ControladorBase
{
    public SedesController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index() => View(await Api.GetAsync<List<SedeDto>>("api/v1/sedes"));

    [HttpGet]
    public async Task<IActionResult> Editar(int id = 0) =>
        View(id == 0 ? new SedeDto() : await Api.GetAsync<SedeDto>($"api/v1/sedes/{id}"));

    [HttpPost]
    public async Task<IActionResult> Editar(int id, SedeDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            if (id == 0) await Api.PostAsync<SedeDto>("api/v1/sedes", dto);
            else await Api.PutAsync<SedeDto>($"api/v1/sedes/{id}", dto);
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            ModelState.AgregarErrores(ex);
            return View(dto);
        }
        Exito("Sede guardada.");
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = Admin)]
public class EspecialidadesController : ControladorBase
{
    public EspecialidadesController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index() => View(await Api.GetAsync<List<EspecialidadDto>>("api/v1/especialidades"));

    [HttpGet]
    public async Task<IActionResult> Editar(int id = 0) =>
        View(id == 0 ? new EspecialidadDto() : await Api.GetAsync<EspecialidadDto>($"api/v1/especialidades/{id}"));

    [HttpPost]
    public async Task<IActionResult> Editar(int id, EspecialidadDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            if (id == 0) await Api.PostAsync<EspecialidadDto>("api/v1/especialidades", dto);
            else await Api.PutAsync<EspecialidadDto>($"api/v1/especialidades/{id}", dto);
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            ModelState.AgregarErrores(ex);
            return View(dto);
        }
        Exito("Especialidad guardada.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await Api.DeleteAsync($"api/v1/especialidades/{id}");
            Exito("Especialidad eliminada.");
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            Aviso(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = Admin)]
public class ConsultoriosController : ControladorBase
{
    public ConsultoriosController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index() => View(await Api.GetAsync<List<ConsultorioDto>>("api/v1/consultorios"));

    [HttpGet]
    public async Task<IActionResult> Editar(int id = 0)
    {
        await CargarSedesAsync();
        return View(id == 0 ? new ConsultorioDto() : await Api.GetAsync<ConsultorioDto>($"api/v1/consultorios/{id}"));
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, ConsultorioDto dto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (id == 0) await Api.PostAsync<ConsultorioDto>("api/v1/consultorios", dto);
                else await Api.PutAsync<ConsultorioDto>($"api/v1/consultorios/{id}", dto);
                Exito("Consultorio guardado.");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
            {
                ModelState.AgregarErrores(ex);
            }
        }
        await CargarSedesAsync();
        return View(dto);
    }

    private async Task CargarSedesAsync()
    {
        var sedes = await Api.GetAsync<List<SedeDto>>("api/v1/sedes");
        ViewBag.Sedes = new SelectList(sedes.Where(s => s.Estado), nameof(SedeDto.IdSede), nameof(SedeDto.Nombre));
    }
}
