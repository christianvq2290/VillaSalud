using Microsoft.AspNetCore.Identity;
using VillaSalud.Application.Abstracciones;

namespace VillaSalud.Infrastructure.Servicios;

/// <summary>
/// Hash de contraseñas con el PasswordHasher de ASP.NET Core Identity: PBKDF2 con sal aleatoria
/// por usuario. El hash incluye la sal y el número de iteraciones, por lo que es portable entre servidores.
/// </summary>
public class HasherClaves : IHasherClaves
{
    private static readonly object Marcador = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string clave) => _hasher.HashPassword(Marcador, clave);

    public bool Verificar(string hash, string clave)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(clave)) return false;
        try
        {
            return _hasher.VerifyHashedPassword(Marcador, hash, clave) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;   // el valor guardado no es un hash válido
        }
    }
}
