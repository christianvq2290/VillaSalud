using System.Text.Json;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Application.Servicios;

public interface IAuditoriaService
{
    /// <summary>Agrega el registro a la unidad de trabajo; se guarda junto con la operación auditada.</summary>
    void Registrar(string accion, string entidad, string? idEntidad = null, object? valores = null, int? idUsuario = null);
    Task<List<AuditoriaDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);
}

public class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaRepositorio _repositorio;
    private readonly IContextoUsuario _contexto;
    private readonly IReloj _reloj;

    public AuditoriaService(IAuditoriaRepositorio repositorio, IContextoUsuario contexto, IReloj reloj)
    {
        _repositorio = repositorio;
        _contexto = contexto;
        _reloj = reloj;
    }

    public void Registrar(string accion, string entidad, string? idEntidad = null, object? valores = null, int? idUsuario = null)
    {
        _repositorio.Agregar(new Auditoria
        {
            IdUsuario = idUsuario ?? _contexto.IdUsuario,
            Accion = accion,
            Entidad = entidad,
            IdEntidad = idEntidad,
            ValoresJson = valores is null ? null : JsonSerializer.Serialize(valores),
            Ip = _contexto.Ip,
            Fecha = _reloj.Ahora
        });
    }

    public async Task<List<AuditoriaDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(_reloj.Ahora);
        var inicio = (desde ?? hoy.AddDays(-7)).ToDateTime(TimeOnly.MinValue);
        var fin = (hasta ?? hoy).AddDays(1).ToDateTime(TimeOnly.MinValue);
        var registros = await _repositorio.ListarAsync(inicio, fin, 500, ct);
        return registros.Select(a => new AuditoriaDto
        {
            Fecha = a.Fecha, Usuario = a.Usuario?.Username, Accion = a.Accion, Entidad = a.Entidad,
            IdEntidad = a.IdEntidad, Detalle = a.ValoresJson, Ip = a.Ip
        }).ToList();
    }
}
