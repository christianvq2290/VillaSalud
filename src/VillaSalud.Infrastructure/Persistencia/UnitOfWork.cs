using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Infrastructure.Persistencia;

public class UnitOfWork : IUnitOfWork
{
    private const int ErrorIndiceUnico = 2601, ErrorClaveUnica = 2627, ErrorInterbloqueo = 1205;

    private readonly VillaSaludDbContext _db;

    public UnitOfWork(VillaSaludDbContext db) => _db = db;

    public async Task<ITransaccion> IniciarTransaccionAsync(bool serializable = false, CancellationToken ct = default)
    {
        var nivel = serializable ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;
        return new Transaccion(await _db.Database.BeginTransactionAsync(nivel, ct));
    }

    /// <summary>Guarda los cambios y traduce los errores de concurrencia de SQL Server a excepciones del dominio (409).</summary>
    public async Task GuardarAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // RN09: row_version cambió entre la lectura y la escritura
            throw new ConflictoException("El registro fue modificado por otro usuario. Vuelva a cargarlo e intente de nuevo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            if (sql.Number == ErrorInterbloqueo || (EsDuplicado(sql) && sql.Message.Contains("UX_CITA_")))
                // RN04: otro usuario tomó el mismo intervalo en el mismo instante
                throw new ConflictoException("El médico o el consultorio ya tiene una cita en ese horario.");
            if (EsDuplicado(sql))
                throw new ConflictoException("Ya existe un registro con esos datos.");
            throw;
        }
    }

    private static bool EsDuplicado(SqlException sql) => sql.Number is ErrorIndiceUnico or ErrorClaveUnica;

    private sealed class Transaccion : ITransaccion
    {
        private readonly IDbContextTransaction _tx;
        public Transaccion(IDbContextTransaction tx) => _tx = tx;
        public Task ConfirmarAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);
        // Si no se confirmó, al liberar la transacción SQL Server deshace los cambios
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }
}
