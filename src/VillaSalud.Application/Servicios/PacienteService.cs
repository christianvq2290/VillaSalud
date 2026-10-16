using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Application.Servicios;

public class PacienteService
{
    private readonly IPacienteRepositorio _pacientes;
    private readonly IUnitOfWork _uow;
    private readonly IReloj _reloj;
    private readonly IAuditoriaService _auditoria;

    public PacienteService(IPacienteRepositorio pacientes, IUnitOfWork uow, IReloj reloj, IAuditoriaService auditoria)
    {
        _pacientes = pacientes;
        _uow = uow;
        _reloj = reloj;
        _auditoria = auditoria;
    }

    public async Task<List<PacienteDto>> ListarAsync(string? busqueda = null, CancellationToken ct = default) =>
        (await _pacientes.ListarAsync(string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim(), ct)).Select(p => p.ADto()).ToList();

    public async Task<PacienteDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        (await _pacientes.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Paciente no encontrado.")).ADto();

    public async Task<PacienteDto> GuardarAsync(int id, PacienteDto dto, CancellationToken ct = default)
    {
        var dni = dto.Dni.Trim();
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (await _pacientes.ExisteDniAsync(dni, id == 0 ? null : id, ct))
            throw new ReglaNegocioException("El DNI ya está registrado.");
        if (email is not null && await _pacientes.ExisteEmailAsync(email, id == 0 ? null : id, ct))
            throw new ReglaNegocioException("El correo ya está registrado para otro paciente.");
        if (dto.FechaNacimiento.HasValue && dto.FechaNacimiento.Value > DateOnly.FromDateTime(_reloj.Ahora))
            throw new ReglaNegocioException("La fecha de nacimiento no puede ser futura.");

        Paciente paciente;
        if (id == 0)
        {
            paciente = new Paciente();
            _pacientes.Agregar(paciente);
        }
        else
        {
            paciente = await _pacientes.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Paciente no encontrado.");
        }
        paciente.Dni = dni;
        paciente.Nombres = dto.Nombres.Trim();
        paciente.Apellidos = dto.Apellidos.Trim();
        paciente.FechaNacimiento = dto.FechaNacimiento;
        paciente.Telefono = Limpiar(dto.Telefono);
        paciente.Email = email;
        paciente.Direccion = Limpiar(dto.Direccion);
        paciente.TipoSangre = Limpiar(dto.TipoSangre);
        paciente.Alergias = Limpiar(dto.Alergias);

        // Consentimiento (Ley N.° 29733): se registra al aceptar y se revoca al retirar la aceptación.
        if (dto.AceptaTratamientoDatos && !paciente.TieneConsentimientoVigente)
        {
            paciente.Consentimientos.Add(new Consentimiento { FechaAceptacion = _reloj.Ahora, Canal = "Presencial" });
        }
        else if (!dto.AceptaTratamientoDatos && paciente.TieneConsentimientoVigente)
        {
            foreach (var c in paciente.Consentimientos.Where(c => !c.Revocado)) c.Revocado = true;
        }

        await _uow.GuardarAsync(ct);
        // En la auditoría no se guardan datos de salud, solo qué registro cambió.
        _auditoria.Registrar(id == 0 ? "CREAR" : "MODIFICAR", "PACIENTE", paciente.IdPaciente.ToString(),
            new { consentimiento = paciente.TieneConsentimientoVigente });
        await _uow.GuardarAsync(ct);
        return paciente.ADto();
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
