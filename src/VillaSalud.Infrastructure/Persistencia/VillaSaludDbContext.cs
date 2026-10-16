using Microsoft.EntityFrameworkCore;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Infrastructure.Persistencia;

public class VillaSaludDbContext : DbContext
{
    public VillaSaludDbContext(DbContextOptions<VillaSaludDbContext> options) : base(options) { }

    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Consultorio> Consultorios => Set<Consultorio>();
    public DbSet<HorarioMedico> Horarios => Set<HorarioMedico>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();
    public DbSet<EstadoCita> EstadosCita => Set<EstadoCita>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<HistorialCita> HistorialCitas => Set<HistorialCita>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carga todas las clases IEntityTypeConfiguration de este ensamblado (archivo Configuraciones.cs)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VillaSaludDbContext).Assembly);
    }
}
