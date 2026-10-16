using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Models;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

[Authorize(Roles = Admin)]
public class UsuariosController : ControladorBase
{
    public UsuariosController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index() => View(await Api.GetAsync<List<UsuarioDto>>("api/v1/usuarios"));

    [HttpGet]
    public async Task<IActionResult> Editar(int id = 0)
    {
        var vm = new UsuarioEditarVm { IdUsuario = id };
        if (id != 0)
        {
            var u = await Api.GetAsync<UsuarioDto>($"api/v1/usuarios/{id}");
            vm.Datos = new UsuarioGuardarDto { Username = u.Username, IdRol = u.IdRol, IdSede = u.IdSede, IdMedico = u.IdMedico, Estado = u.Estado };
        }
        await CargarListasAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, UsuarioEditarVm vm)
    {
        vm.IdUsuario = id;
        if (id == 0 && string.IsNullOrWhiteSpace(vm.Datos.Password))
            ModelState.AddModelError("Datos.Password", "Ingrese una contraseña temporal.");

        if (ModelState.IsValid)
        {
            try
            {
                if (id == 0) await Api.PostAsync<UsuarioDto>("api/v1/usuarios", vm.Datos);
                else await Api.PutAsync<UsuarioDto>($"api/v1/usuarios/{id}", vm.Datos);
                Exito("Usuario guardado.");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex) when (!ex.EsNoAutenticado && !ex.EsAccesoDenegado)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        await CargarListasAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Desbloquear(int id)
    {
        await Api.PostAsync($"api/v1/usuarios/{id}/desbloquear");
        Exito("Usuario desbloqueado.");
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasAsync(UsuarioEditarVm vm)
    {
        vm.Roles = await Api.GetAsync<List<RolDto>>("api/v1/usuarios/roles");
        vm.Sedes = (await Api.GetAsync<List<SedeDto>>("api/v1/sedes")).Where(s => s.Estado).ToList();
        vm.Medicos = await Api.GetAsync<List<MedicoDto>>("api/v1/medicos?soloActivos=true");
    }
}

[Authorize(Roles = Admin)]
public class AuditoriaController : ControladorBase
{
    public AuditoriaController(ApiCliente api) : base(api) { }

    public async Task<IActionResult> Index(DateOnly? desde = null, DateOnly? hasta = null)
    {
        var vm = new AuditoriaVm { Desde = desde ?? Hoy.AddDays(-7), Hasta = hasta ?? Hoy };
        vm.Registros = await Api.GetAsync<List<AuditoriaDto>>(Ruta("api/v1/auditoria", ("desde", vm.Desde), ("hasta", vm.Hasta)));
        return View(vm);
    }
}
