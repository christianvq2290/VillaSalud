using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.UnitTests.Dobles;

public class RelojFijo : IReloj
{
    public RelojFijo(DateTime ahora) => Ahora = ahora;
    public DateTime Ahora { get; set; }
}

public class ContextoFijo : IContextoUsuario
{
    public int? IdUsuario { get; set; }
    public string? Rol { get; set; }
    public int? IdSede { get; set; }
    public int? IdMedico { get; set; }
    public string? Ip { get; set; } = "127.0.0.1";
}

/// <summary>Hasher trivial para las pruebas unitarias (el real, PBKDF2, se prueba en las pruebas de integración).</summary>
public class HasherDePrueba : IHasherClaves
{
    public string Hash(string clave) => "hash:" + clave;
    public bool Verificar(string hash, string clave) => hash == "hash:" + clave;
}

public class GeneradorTokensDePrueba : IGeneradorTokens
{
    public TokenAcceso CrearTokenAcceso(Usuario usuario) => new($"token-{usuario.Username}-{Guid.NewGuid():N}", DateTime.UtcNow.AddMinutes(30));
}

public class CorreoDePrueba : IEnviadorCorreo
{
    public bool Configurado { get; set; } = true;
    public bool Fallar { get; set; }
    public List<(string Destino, string Asunto, string Cuerpo)> Enviados { get; } = new();

    public Task EnviarAsync(string destino, string asunto, string cuerpo, CancellationToken ct = default)
    {
        if (Fallar) throw new InvalidOperationException("Servidor SMTP no disponible");
        Enviados.Add((destino, asunto, cuerpo));
        return Task.CompletedTask;
    }
}

public class ExportadorDePrueba : IExportadorReporte
{
    public string Formato => "csv";
    public string Mime => "text/csv";
    public byte[] Generar(ReporteCitasDto reporte) => System.Text.Encoding.UTF8.GetBytes(string.Join("\n", reporte.Filas.Select(f => f.Codigo)));
}
