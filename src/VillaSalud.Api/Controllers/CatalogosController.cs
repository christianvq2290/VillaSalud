using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

[ApiController]
[Route("api/v1/sedes")]
[Authorize]
public class SedesController : ControllerBase
{
    private readonly CatalogoService _catalogo;
    public SedesController(CatalogoService catalogo) => _catalogo = catalogo;

    [HttpGet]
    public async Task<ActionResult<List<SedeDto>>> Listar(CancellationToken ct) => Ok(await _catalogo.ListarSedesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SedeDto>> Obtener(int id, CancellationToken ct) => Ok(await _catalogo.ObtenerSedeAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<SedeDto>> Crear(SedeDto dto, CancellationToken ct)
    {
        var creada = await _catalogo.GuardarSedeAsync(0, dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creada.IdSede }, creada);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<SedeDto>> Actualizar(int id, SedeDto dto, CancellationToken ct) =>
        Ok(await _catalogo.GuardarSedeAsync(id, dto, ct));
}

[ApiController]
[Route("api/v1/especialidades")]
[Authorize]
public class EspecialidadesController : ControllerBase
{
    private readonly CatalogoService _catalogo;
    public EspecialidadesController(CatalogoService catalogo) => _catalogo = catalogo;

    [HttpGet]
    public async Task<ActionResult<List<EspecialidadDto>>> Listar(CancellationToken ct) => Ok(await _catalogo.ListarEspecialidadesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EspecialidadDto>> Obtener(int id, CancellationToken ct) => Ok(await _catalogo.ObtenerEspecialidadAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<EspecialidadDto>> Crear(EspecialidadDto dto, CancellationToken ct)
    {
        var creada = await _catalogo.GuardarEspecialidadAsync(0, dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creada.IdEspecialidad }, creada);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<EspecialidadDto>> Actualizar(int id, EspecialidadDto dto, CancellationToken ct) =>
        Ok(await _catalogo.GuardarEspecialidadAsync(id, dto, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _catalogo.EliminarEspecialidadAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/consultorios")]
[Authorize]
public class ConsultoriosController : ControllerBase
{
    private readonly CatalogoService _catalogo;
    public ConsultoriosController(CatalogoService catalogo) => _catalogo = catalogo;

    [HttpGet]
    public async Task<ActionResult<List<ConsultorioDto>>> Listar([FromQuery] int? idSede, CancellationToken ct) =>
        Ok(await _catalogo.ListarConsultoriosAsync(idSede, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConsultorioDto>> Obtener(int id, CancellationToken ct) => Ok(await _catalogo.ObtenerConsultorioAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<ConsultorioDto>> Crear(ConsultorioDto dto, CancellationToken ct)
    {
        var creado = await _catalogo.GuardarConsultorioAsync(0, dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdConsultorio }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<ConsultorioDto>> Actualizar(int id, ConsultorioDto dto, CancellationToken ct) =>
        Ok(await _catalogo.GuardarConsultorioAsync(id, dto, ct));
}
