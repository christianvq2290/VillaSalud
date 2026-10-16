using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Models;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

[Authorize(Roles = AdminORecepcion)]
public class PacientesController : ControladorBase
{
    public PacientesController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index(string? busqueda = null) => View(new PacientesIndexVm
    {
        Busqueda = busqueda,
        Pacientes = await Api.GetAsync<List<PacienteDto>>(Ruta("api/v1/pacientes", ("busqueda", busqueda)))
    });

    [HttpGet]
    public async Task<IActionResult> Editar(int id = 0, string? dni = null) =>
        View(id == 0 ? new PacienteDto { Dni = dni ?? "" } : await Api.GetAsync<PacienteDto>($"api/v1/pacientes/{id}"));

    [HttpPost]
    public async Task<IActionResult> Editar(int id, PacienteDto dto, bool programar = false)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            if (id == 0) await Api.PostAsync<PacienteDto>("api/v1/pacientes", dto);
            else await Api.PutAsync<PacienteDto>($"api/v1/pacientes/{id}", dto);
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            ModelState.AgregarErrores(ex);
            return View(dto);
        }
        Exito("Paciente guardado.");
        return programar ? RedirectToAction("Programar", "Citas", new { dni = dto.Dni }) : RedirectToAction(nameof(Index));
    }
}
