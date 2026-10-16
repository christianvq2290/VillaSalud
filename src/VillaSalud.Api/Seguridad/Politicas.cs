using VillaSalud.Domain.Entidades;

namespace VillaSalud.Api.Seguridad;

/// <summary>Nombres de las políticas de autorización y de los límites de peticiones.</summary>
public static class Politicas
{
    public const string SoloAdmin = "SoloAdmin";
    public const string GestionCitas = "GestionCitas";
    public const string Agenda = "Agenda";

    public const string LimiteLogin = "login";
    public const string LimitePublico = "publico";

    public static readonly string[] RolesGestionCitas = { Roles.Administrador, Roles.Recepcionista };
    public static readonly string[] RolesAgenda = { Roles.Administrador, Roles.Medico };
}

/// <summary>Nombres de los claims que viajan en el token JWT.</summary>
public static class ClaimsToken
{
    public const string Id = "sub";
    public const string Nombre = "name";
    public const string Rol = "role";
    public const string Sede = "sede";
    public const string Medico = "medico";
}
