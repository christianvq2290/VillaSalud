using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Application.Servicios;

/// <summary>Datos maestros simples: sedes, especialidades y consultorios.</summary>
public class CatalogoService
{
    private readonly ICatalogoRepositorio _catalogo;
    private readonly IUnitOfWork _uow;
    private readonly IContextoUsuario _contexto;
    private readonly IAuditoriaService _auditoria;

    public CatalogoService(ICatalogoRepositorio catalogo, IUnitOfWork uow, IContextoUsuario contexto, IAuditoriaService auditoria)
    {
        _catalogo = catalogo;
        _uow = uow;
        _contexto = contexto;
        _auditoria = auditoria;
    }

    // ---------- Sedes ----------
    public async Task<List<SedeDto>> ListarSedesAsync(CancellationToken ct = default) =>
        (await _catalogo.ListarSedesAsync(ct)).Select(s => s.ADto()).ToList();

    public async Task<SedeDto> ObtenerSedeAsync(int id, CancellationToken ct = default) =>
        (await _catalogo.ObtenerSedeAsync(id, ct) ?? throw new NoEncontradoException("Sede no encontrada.")).ADto();

    public async Task<SedeDto> GuardarSedeAsync(int id, SedeDto dto, CancellationToken ct = default)
    {
        var nombre = dto.Nombre.Trim();
        if (await _catalogo.ExisteSedeAsync(nombre, id == 0 ? null : id, ct))
            throw new ReglaNegocioException("Ya existe una sede con ese nombre.");

        Sede sede;
        if (id == 0)
        {
            sede = new Sede();
            _catalogo.AgregarSede(sede);
        }
        else
        {
            sede = await _catalogo.ObtenerSedeAsync(id, ct) ?? throw new NoEncontradoException("Sede no encontrada.");
        }
        sede.Nombre = nombre;
        sede.Direccion = dto.Direccion.Trim();
        sede.Distrito = dto.Distrito.Trim();
        sede.Estado = dto.Estado;
        await _uow.GuardarAsync(ct);
        _auditoria.Registrar(id == 0 ? "CREAR" : "MODIFICAR", "SEDE", sede.IdSede.ToString(), new { sede.Nombre });
        await _uow.GuardarAsync(ct);
        return sede.ADto();
    }

    // ---------- Especialidades ----------
    public async Task<List<EspecialidadDto>> ListarEspecialidadesAsync(CancellationToken ct = default) =>
        (await _catalogo.ListarEspecialidadesAsync(ct)).Select(e => e.ADto()).ToList();

    public async Task<EspecialidadDto> ObtenerEspecialidadAsync(int id, CancellationToken ct = default) =>
        (await _catalogo.ObtenerEspecialidadAsync(id, ct) ?? throw new NoEncontradoException("Especialidad no encontrada.")).ADto();

    public async Task<EspecialidadDto> GuardarEspecialidadAsync(int id, EspecialidadDto dto, CancellationToken ct = default)
    {
        var nombre = dto.Nombre.Trim();
        if (await _catalogo.ExisteEspecialidadAsync(nombre, id == 0 ? null : id, ct))
            throw new ReglaNegocioException("Ya existe una especialidad con ese nombre.");

        Especialidad especialidad;
        if (id == 0)
        {
            especialidad = new Especialidad();
            _catalogo.AgregarEspecialidad(especialidad);
        }
        else
        {
            especialidad = await _catalogo.ObtenerEspecialidadAsync(id, ct) ?? throw new NoEncontradoException("Especialidad no encontrada.");
        }
        especialidad.Nombre = nombre;
        await _uow.GuardarAsync(ct);
        return especialidad.ADto();
    }

    public async Task EliminarEspecialidadAsync(int id, CancellationToken ct = default)
    {
        var especialidad = await _catalogo.ObtenerEspecialidadAsync(id, ct) ?? throw new NoEncontradoException("Especialidad no encontrada.");
        if (await _catalogo.EspecialidadTieneMedicosAsync(id, ct))
            throw new ReglaNegocioException("No se puede eliminar: hay médicos registrados con esta especialidad.");
        _catalogo.EliminarEspecialidad(especialidad);
        _auditoria.Registrar("ELIMINAR", "ESPECIALIDAD", id.ToString(), new { especialidad.Nombre });
        await _uow.GuardarAsync(ct);
    }

    // ---------- Consultorios ----------
    /// <summary>RN01: un usuario con sede solo ve los consultorios de su sede.</summary>
    public async Task<List<ConsultorioDto>> ListarConsultoriosAsync(int? idSede = null, CancellationToken ct = default) =>
        (await _catalogo.ListarConsultoriosAsync(_contexto.IdSede ?? idSede, ct)).Select(c => c.ADto()).ToList();

    public async Task<ConsultorioDto> ObtenerConsultorioAsync(int id, CancellationToken ct = default) =>
        (await _catalogo.ObtenerConsultorioAsync(id, ct) ?? throw new NoEncontradoException("Consultorio no encontrado.")).ADto();

    public async Task<ConsultorioDto> GuardarConsultorioAsync(int id, ConsultorioDto dto, CancellationToken ct = default)
    {
        var sede = await _catalogo.ObtenerSedeAsync(dto.IdSede, ct) ?? throw new ReglaNegocioException("La sede seleccionada no existe.");
        var numero = dto.Numero.Trim();
        if (await _catalogo.ExisteConsultorioAsync(dto.IdSede, numero, id == 0 ? null : id, ct))
            throw new ReglaNegocioException("Ya existe un consultorio con ese número en la sede.");

        Consultorio consultorio;
        if (id == 0)
        {
            consultorio = new Consultorio();
            _catalogo.AgregarConsultorio(consultorio);
        }
        else
        {
            consultorio = await _catalogo.ObtenerConsultorioAsync(id, ct) ?? throw new NoEncontradoException("Consultorio no encontrado.");
        }
        consultorio.IdSede = dto.IdSede;
        consultorio.Sede = sede;
        consultorio.Numero = numero;
        consultorio.Piso = string.IsNullOrWhiteSpace(dto.Piso) ? null : dto.Piso.Trim();
        consultorio.Estado = dto.Estado;
        await _uow.GuardarAsync(ct);
        return consultorio.ADto();
    }
}
