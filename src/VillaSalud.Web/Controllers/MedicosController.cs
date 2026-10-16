using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Models;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

[Authorize(Roles = Admin)]
public class MedicosController : ControladorBase
{
    public MedicosController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index() => View(await Api.GetAsync<List<MedicoDto>>("api/v1/medicos"));

    [HttpGet]
    public async Task<IActionResult> Editar(int id = 0)
    {
        await CargarEspecialidadesAsync();
        return View(id == 0 ? new MedicoDto() : await Api.GetAsync<MedicoDto>($"api/v1/medicos/{id}"));
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, MedicoDto dto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (id == 0) await Api.PostAsync<MedicoDto>("api/v1/medicos", dto);
                else await Api.PutAsync<MedicoDto>($"api/v1/medicos/{id}", dto);
                Exito("Médico guardado.");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
            {
                ModelState.AgregarErrores(ex);
            }
        }
        await CargarEspecialidadesAsync();
        return View(dto);
    }

    // ---------- Horarios de atención ----------
    [HttpGet]
    public async Task<IActionResult> Horarios(int id) => View(await CrearHorariosVmAsync(id, new HorarioDto()));

    [HttpPost]
    public async Task<IActionResult> AgregarHorario(int id, [Bind(Prefix = "Nuevo")] HorarioDto nuevo)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await Api.PostAsync<HorarioDto>($"api/v1/medicos/{id}/horarios", nuevo);
                Exito("Horario agregado.");
                return RedirectToAction(nameof(Horarios), new { id });
            }
            catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
            {
                ModelState.AgregarErrores(ex);
            }
        }
        return View(nameof(Horarios), await CrearHorariosVmAsync(id, nuevo));
    }

    [HttpPost]
    public async Task<IActionResult> QuitarHorario(int id, int idHorario)
    {
        await Api.DeleteAsync($"api/v1/medicos/{id}/horarios/{idHorario}");
        Exito("Horario retirado.");
        return RedirectToAction(nameof(Horarios), new { id });
    }

    private async Task<HorariosVm> CrearHorariosVmAsync(int idMedico, HorarioDto nuevo) => new()
    {
        Medico = await Api.GetAsync<MedicoDto>($"api/v1/medicos/{idMedico}"),
        Horarios = await Api.GetAsync<List<HorarioDto>>($"api/v1/medicos/{idMedico}/horarios"),
        Consultorios = (await Api.GetAsync<List<ConsultorioDto>>("api/v1/consultorios")).Where(c => c.Estado).ToList(),
        Nuevo = nuevo
    };

    private async Task CargarEspecialidadesAsync()
    {
        var especialidades = await Api.GetAsync<List<EspecialidadDto>>("api/v1/especialidades");
        ViewBag.Especialidades = new SelectList(especialidades, nameof(EspecialidadDto.IdEspecialidad), nameof(EspecialidadDto.Nombre));
    }
}
