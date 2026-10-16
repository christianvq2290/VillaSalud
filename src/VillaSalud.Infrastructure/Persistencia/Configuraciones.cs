using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Infrastructure.Persistencia;

// Mapeo de cada entidad a su tabla. Los nombres de tabla y columna coinciden con bd/01_creacion_bd.sql.
// No se configuran valores por defecto en EF: la aplicación siempre envía el valor (así un "false" no se pierde).

public class SedeConfig : IEntityTypeConfiguration<Sede>
{
    public void Configure(EntityTypeBuilder<Sede> b)
    {
        b.ToTable("SEDE");
        b.HasKey(x => x.IdSede);
        b.Property(x => x.IdSede).HasColumnName("id_sede");
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsUnicode(false).IsRequired();
        b.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(150).IsUnicode(false).IsRequired();
        b.Property(x => x.Distrito).HasColumnName("distrito").HasMaxLength(60).IsUnicode(false).IsRequired();
        b.Property(x => x.Estado).HasColumnName("estado");
    }
}

public class RolConfig : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.ToTable("ROL");
        b.HasKey(x => x.IdRol);
        b.Property(x => x.IdRol).HasColumnName("id_rol");
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsUnicode(false).IsRequired();
    }
}

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("USUARIO");
        b.HasKey(x => x.IdUsuario);
        b.Property(x => x.IdUsuario).HasColumnName("id_usuario");
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(30).IsUnicode(false).IsRequired();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(x => x.IdRol).HasColumnName("id_rol");
        b.Property(x => x.IdSede).HasColumnName("id_sede");
        b.Property(x => x.IdMedico).HasColumnName("id_medico");
        b.Property(x => x.IntentosFallidos).HasColumnName("intentos_fallidos");
        b.Property(x => x.BloqueadoHasta).HasColumnName("bloqueado_hasta").HasColumnType("datetime2(0)");
        b.Property(x => x.DebeCambiarClave).HasColumnName("debe_cambiar_clave");
        b.Property(x => x.Estado).HasColumnName("estado");
        b.HasIndex(x => x.Username).IsUnique();
        b.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.IdRol).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Sede).WithMany().HasForeignKey(x => x.IdSede).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Medico).WithMany().HasForeignKey(x => x.IdMedico).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("REFRESH_TOKEN");
        b.HasKey(x => x.IdToken);
        b.Property(x => x.IdToken).HasColumnName("id_token");
        b.Property(x => x.IdUsuario).HasColumnName("id_usuario");
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsUnicode(false).IsRequired();
        b.Property(x => x.Expira).HasColumnName("expira").HasColumnType("datetime2(0)");
        b.Property(x => x.Revocado).HasColumnName("revocado");
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EspecialidadConfig : IEntityTypeConfiguration<Especialidad>
{
    public void Configure(EntityTypeBuilder<Especialidad> b)
    {
        b.ToTable("ESPECIALIDAD");
        b.HasKey(x => x.IdEspecialidad);
        b.Property(x => x.IdEspecialidad).HasColumnName("id_especialidad");
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsUnicode(false).IsRequired();
    }
}

public class MedicoConfig : IEntityTypeConfiguration<Medico>
{
    public void Configure(EntityTypeBuilder<Medico> b)
    {
        b.ToTable("MEDICO");
        b.HasKey(x => x.IdMedico);
        b.Ignore(x => x.NombreCompleto);
        b.Property(x => x.IdMedico).HasColumnName("id_medico");
        b.Property(x => x.CodigoColegiatura).HasColumnName("codigo_colegiatura").HasMaxLength(50).IsUnicode(false).IsRequired();
        b.Property(x => x.Nombres).HasColumnName("nombres").HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.Apellidos).HasColumnName("apellidos").HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.IdEspecialidad).HasColumnName("id_especialidad");
        b.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.Estado).HasColumnName("estado");
        b.HasOne(x => x.Especialidad).WithMany().HasForeignKey(x => x.IdEspecialidad).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConsultorioConfig : IEntityTypeConfiguration<Consultorio>
{
    public void Configure(EntityTypeBuilder<Consultorio> b)
    {
        b.ToTable("CONSULTORIO");
        b.HasKey(x => x.IdConsultorio);
        b.Property(x => x.IdConsultorio).HasColumnName("id_consultorio");
        b.Property(x => x.IdSede).HasColumnName("id_sede");
        b.Property(x => x.Numero).HasColumnName("numero").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.Piso).HasColumnName("piso").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.Estado).HasColumnName("estado");
        b.HasOne(x => x.Sede).WithMany().HasForeignKey(x => x.IdSede).OnDelete(DeleteBehavior.Restrict);
    }
}

public class HorarioMedicoConfig : IEntityTypeConfiguration<HorarioMedico>
{
    public void Configure(EntityTypeBuilder<HorarioMedico> b)
    {
        b.ToTable("HORARIO_MEDICO");
        b.HasKey(x => x.IdHorario);
        b.Property(x => x.IdHorario).HasColumnName("id_horario");
        b.Property(x => x.IdMedico).HasColumnName("id_medico");
        b.Property(x => x.IdConsultorio).HasColumnName("id_consultorio");
        b.Property(x => x.DiaSemana).HasColumnName("dia_semana");
        b.Property(x => x.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time(0)");
        b.Property(x => x.HoraFin).HasColumnName("hora_fin").HasColumnType("time(0)");
        b.Property(x => x.DuracionCitaMin).HasColumnName("duracion_cita_min");
        b.Property(x => x.Vigente).HasColumnName("vigente");
        b.HasOne(x => x.Medico).WithMany().HasForeignKey(x => x.IdMedico).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Consultorio).WithMany().HasForeignKey(x => x.IdConsultorio).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PacienteConfig : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> b)
    {
        b.ToTable("PACIENTE");
        b.HasKey(x => x.IdPaciente);
        b.Ignore(x => x.NombreCompleto);
        b.Ignore(x => x.TieneConsentimientoVigente);
        b.Property(x => x.IdPaciente).HasColumnName("id_paciente");
        b.Property(x => x.Dni).HasColumnName("dni").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.Nombres).HasColumnName("nombres").HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.Apellidos).HasColumnName("apellidos").HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.FechaNacimiento).HasColumnName("fecha_nacimiento").HasColumnType("date");
        b.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(200).IsUnicode(false);
        b.Property(x => x.TipoSangre).HasColumnName("tipo_sangre").HasMaxLength(5).IsUnicode(false);
        b.Property(x => x.Alergias).HasColumnName("alergias").HasMaxLength(500).IsUnicode(false);
        b.HasIndex(x => x.Dni).IsUnique();
        b.HasMany(x => x.Consentimientos).WithOne().HasForeignKey(x => x.IdPaciente).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConsentimientoConfig : IEntityTypeConfiguration<Consentimiento>
{
    public void Configure(EntityTypeBuilder<Consentimiento> b)
    {
        b.ToTable("CONSENTIMIENTO");
        b.HasKey(x => x.IdConsentimiento);
        b.Property(x => x.IdConsentimiento).HasColumnName("id_consentimiento");
        b.Property(x => x.IdPaciente).HasColumnName("id_paciente");
        b.Property(x => x.Finalidad).HasColumnName("finalidad").HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(x => x.VersionTexto).HasColumnName("version_texto").HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.FechaAceptacion).HasColumnName("fecha_aceptacion").HasColumnType("datetime2(0)");
        b.Property(x => x.Canal).HasColumnName("canal").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Revocado).HasColumnName("revocado");
    }
}

public class EstadoCitaConfig : IEntityTypeConfiguration<EstadoCita>
{
    public void Configure(EntityTypeBuilder<EstadoCita> b)
    {
        b.ToTable("ESTADO_CITA");
        b.HasKey(x => x.IdEstadoCita);
        b.Property(x => x.IdEstadoCita).HasColumnName("id_estado_cita");
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsUnicode(false).IsRequired();
    }
}

public class CitaConfig : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> b)
    {
        b.ToTable("CITA");
        b.HasKey(x => x.IdCita);
        b.Ignore(x => x.EstaActiva);
        b.Property(x => x.IdCita).HasColumnName("id_cita");
        b.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(12).IsUnicode(false).IsRequired();
        b.Property(x => x.IdSede).HasColumnName("id_sede");
        b.Property(x => x.IdPaciente).HasColumnName("id_paciente");
        b.Property(x => x.IdMedico).HasColumnName("id_medico");
        b.Property(x => x.IdConsultorio).HasColumnName("id_consultorio");
        b.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("date");
        b.Property(x => x.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time(0)");
        b.Property(x => x.HoraFin).HasColumnName("hora_fin").HasColumnType("time(0)");
        b.Property(x => x.MotivoConsulta).HasColumnName("motivo_consulta").HasMaxLength(500).IsUnicode(false);
        b.Property(x => x.IdEstadoCita).HasColumnName("id_estado_cita");
        b.Property(x => x.Observacion).HasColumnName("observacion").HasMaxLength(250).IsUnicode(false);
        b.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").HasColumnType("datetime");
        b.Property(x => x.IdUsuarioRegistro).HasColumnName("id_usuario_registro");
        // RN09: SQL Server cambia row_version en cada UPDATE; EF la compara al guardar
        b.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasOne(x => x.Sede).WithMany().HasForeignKey(x => x.IdSede).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Paciente).WithMany().HasForeignKey(x => x.IdPaciente).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Medico).WithMany().HasForeignKey(x => x.IdMedico).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Consultorio).WithMany().HasForeignKey(x => x.IdConsultorio).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EstadoCita).WithMany().HasForeignKey(x => x.IdEstadoCita).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Historial).WithOne().HasForeignKey(x => x.IdCita).OnDelete(DeleteBehavior.Cascade);
    }
}

public class HistorialCitaConfig : IEntityTypeConfiguration<HistorialCita>
{
    public void Configure(EntityTypeBuilder<HistorialCita> b)
    {
        b.ToTable("HISTORIAL_CITA");
        b.HasKey(x => x.IdHistorial);
        b.Property(x => x.IdHistorial).HasColumnName("id_historial");
        b.Property(x => x.IdCita).HasColumnName("id_cita");
        b.Property(x => x.IdEstadoAnterior).HasColumnName("id_estado_anterior");
        b.Property(x => x.IdEstadoNuevo).HasColumnName("id_estado_nuevo");
        b.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(250).IsUnicode(false);
        b.Property(x => x.IdUsuario).HasColumnName("id_usuario");
        b.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("datetime2(0)");
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificacionConfig : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> b)
    {
        b.ToTable("NOTIFICACION");
        b.HasKey(x => x.IdNotificacion);
        b.Property(x => x.IdNotificacion).HasColumnName("id_notificacion");
        b.Property(x => x.IdCita).HasColumnName("id_cita");
        b.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Destino).HasColumnName("destino").HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.EstadoEnvio).HasColumnName("estado_envio").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.FechaProgramada).HasColumnName("fecha_programada").HasColumnType("datetime2(0)");
        b.Property(x => x.FechaEnvio).HasColumnName("fecha_envio").HasColumnType("datetime2(0)");
        b.Property(x => x.Detalle).HasColumnName("detalle").HasMaxLength(250).IsUnicode(false);
        b.HasOne(x => x.Cita).WithMany().HasForeignKey(x => x.IdCita).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditoriaConfig : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> b)
    {
        b.ToTable("AUDITORIA");
        b.HasKey(x => x.IdAuditoria);
        b.Property(x => x.IdAuditoria).HasColumnName("id_auditoria");
        b.Property(x => x.IdUsuario).HasColumnName("id_usuario");
        b.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.Entidad).HasColumnName("entidad").HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.IdEntidad).HasColumnName("id_entidad").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.ValoresJson).HasColumnName("valores_json");
        b.Property(x => x.Ip).HasColumnName("ip").HasMaxLength(45).IsUnicode(false);
        b.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("datetime2(0)");
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
    }
}
