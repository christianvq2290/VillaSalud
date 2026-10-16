using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Models;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

[Authorize(Roles = AdminOMedico)]
public class AgendaController : ControladorBase
{
    public AgendaController(ApiCliente api) : base(api) { }

    /// <summary>Agenda del día. El médico ve la suya; el administrador elige el médico.</summary>
    public async Task<IActionResult> Index(DateOnly? fecha = null, int? idMedico = null)
    {
        var vm = new AgendaVm { Fecha = fecha ?? Hoy, EsMedico = EsMedico, IdMedico = EsMedico ? IdMedicoSesion : idMedico };
        if (!EsMedico) vm.Medicos = await Api.GetAsync<List<MedicoDto>>("api/v1/medicos?soloActivos=true");
        if (vm.IdMedico.HasValue)
            vm.Citas = await Api.GetAsync<List<CitaDto>>(Ruta("api/v1/agenda", ("fecha", vm.Fecha), ("idMedico", vm.IdMedico)));
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Atender(int id, AtenderCitaDto dto, DateOnly fecha, int? idMedico = null)
    {
        try
        {
            await Api.PutAsync<CitaDto>($"api/v1/citas/{id}/atender", dto);
            Exito(dto.Asistio ? "Atención registrada." : "Inasistencia registrada.");
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            Aviso(ex.Message);
        }
        return RedirectToAction(nameof(Index), new { fecha = Fecha(fecha), idMedico });
    }
}
