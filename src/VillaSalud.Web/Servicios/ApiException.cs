using System.Net;

namespace VillaSalud.Web.Servicios;

/// <summary>La API respondió con un error. Conserva el código HTTP, el mensaje y los errores de validación por campo.</summary>
public class ApiException : Exception
{
    public HttpStatusCode Estado { get; }
    public IReadOnlyDictionary<string, string[]> Errores { get; }

    public ApiException(HttpStatusCode estado, string mensaje, IReadOnlyDictionary<string, string[]>? errores = null) : base(mensaje)
    {
        Estado = estado;
        Errores = errores ?? new Dictionary<string, string[]>();
    }

    public bool EsNoAutenticado => Estado == HttpStatusCode.Unauthorized;
    public bool EsAccesoDenegado => Estado == HttpStatusCode.Forbidden;
}
