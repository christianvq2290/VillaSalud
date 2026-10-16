using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Models;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

[Authorize(Roles = AdminORecepcion)]
public class CitasController : ControladorBase
{
    public CitasController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index([FromQuery] FiltroCitasDto filtro)
    {
        filtro.Desde ??= Hoy;
        filtro.Hasta ??= filtro.Desde.Value.AddDays(7);
        return View(new CitasIndexVm
        {
            Filtro = filtro,
            Citas = await Api.GetAsync<List<CitaDto>>(RutaCitas("api/v1/citas", filtro)),
            Medicos = await Api.GetAsync<List<MedicoDto>>("api/v1/medicos")
        });
    }

    public async Task<IActionResult> Detalle(int id) => View(await Api.GetAsync<CitaDetalleDto>($"api/v1/citas/{id}"));

    // ---------- Programar: 1) paciente, médico y fecha  2) elegir un intervalo libre ----------
    [HttpGet]
    public async Task<IActionResult> Programar(string? dni = null, int? idMedico = null, DateOnly? fecha = null)
    {
        var vm = new ProgramarCitaVm { Dni = dni?.Trim() };
        vm.Cita.IdMedico = idMedico ?? 0;
        vm.Cita.Fecha = fecha ?? Hoy.AddDays(1);
        await CompletarAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Programar(ProgramarCitaVm vm)
    {
        if (string.IsNullOrEmpty(Request.Form["Cita.HoraInicio"]))
            ModelState.AddModelError(string.Empty, "Seleccione un intervalo disponible.");

        if (ModelState.IsValid)
        {
            try
            {
                var cita = await Api.PostAsync<CitaDto>("api/v1/citas", vm.Cita);
                Exito($"Cita {cita.Codigo} registrada para el {cita.Fecha:dd/MM/yyyy} a las {cita.HoraInicio:HH\\:mm}.");
                return RedirectToAction(nameof(Detalle), new { id = cita.IdCita });
            }
            catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
            {
                // Incluye el 409: otro usuario tomó el intervalo. Se vuelve a mostrar la disponibilidad actualizada.
                ModelState.AgregarErrores(ex);
            }
        }
        await CompletarAsync(vm);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Reprogramar(int id, DateOnly? fecha = null)
    {
        var cita = await Api.GetAsync<CitaDetalleDto>($"api/v1/citas/{id}");
        var vm = new ReprogramarVm { Cita = cita };
        vm.Datos.Fecha = fecha ?? (cita.Fecha > Hoy ? cita.Fecha : Hoy.AddDays(1));
        vm.Datos.RowVersion = cita.RowVersion;
        vm.Intervalos = await DisponibilidadAsync(cita.IdMedico, vm.Datos.Fecha);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Reprogramar(int id, [Bind(Prefix = "Datos")] ReprogramarCitaDto datos)
    {
        if (string.IsNullOrEmpty(Request.Form["Datos.HoraInicio"]))
            ModelState.AddModelError(string.Empty, "Seleccione un intervalo disponible.");

        if (ModelState.IsValid)
        {
            try
            {
                await Api.PutAsync<CitaDto>($"api/v1/citas/{id}/reprogramar", datos);
                Exito("Cita reprogramada.");
                return RedirectToAction(nameof(Detalle), new { id });
            }
            catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
            {
                ModelState.AgregarErrores(ex);
            }
        }
        var cita = await Api.GetAsync<CitaDetalleDto>($"api/v1/citas/{id}");
        return View(new ReprogramarVm { Cita = cita, Datos = datos, Intervalos = await DisponibilidadAsync(cita.IdMedico, datos.Fecha) });
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(int id, CancelarCitaDto dto)
    {
        if (!ModelState.IsValid)
        {
            Aviso("Indique el motivo de la cancelación.");
            return RedirectToAction(nameof(Detalle), new { id });
        }
        try
        {
            await Api.PutAsync<CitaDto>($"api/v1/citas/{id}/cancelar", dto);
            Exito("Cita cancelada.");
        }
        catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
        {
            Aviso(ex.Message);
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task CompletarAsync(ProgramarCitaVm vm)
    {
        vm.Medicos = await Api.GetAsync<List<MedicoDto>>("api/v1/medicos?soloActivos=true");

        if (!string.IsNullOrWhiteSpace(vm.Dni))
        {
            var encontrados = await Api.GetAsync<List<PacienteDto>>(Ruta("api/v1/pacientes", ("busqueda", vm.Dni)));
            vm.Paciente = encontrados.FirstOrDefault(p => p.Dni == vm.Dni);
            if (vm.Paciente is not null) vm.Cita.IdPaciente = vm.Paciente.IdPaciente;
        }

        if (vm.Paciente is not null && vm.Cita.IdMedico > 0)
        {
            vm.Intervalos = await DisponibilidadAsync(vm.Cita.IdMedico, vm.Cita.Fecha);
            vm.MostrarIntervalos = true;
        }
    }

    private Task<List<IntervaloDto>> DisponibilidadAsync(int idMedico, DateOnly fecha) =>
        Api.GetAsync<List<IntervaloDto>>($"api/v1/medicos/{idMedico}/disponibilidad?fecha={Fecha(fecha)}");
}
