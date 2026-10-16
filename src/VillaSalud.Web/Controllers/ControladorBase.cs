using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using VillaSalud.Application.Dtos;
using VillaSalud.Web.Servicios;

namespace VillaSalud.Web.Controllers;

public abstract class ControladorBase : Controller
{
    public const string Admin = "Administrador";
    public const string Medico = "Medico";
    public const string Recepcionista = "Recepcionista";
    public const string AdminORecepcion = Admin + "," + Recepcionista;
    public const string AdminOMedico = Admin + "," + Medico;

    protected ApiCliente Api { get; }

    protected ControladorBase(ApiCliente api) => Api = api;

    protected bool EsAdmin => User.IsInRole(Admin);
    protected bool EsMedico => User.IsInRole(Medico);
    protected int? IdMedicoSesion => int.TryParse(User.FindFirstValue(ServicioSesion.ClaimMedico), out var id) ? id : null;

    /// <summary>Hora de Perú (UTC-5), la misma referencia que usa la API.</summary>
    protected static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

    protected void Exito(string mensaje) => TempData["Exito"] = mensaje;
    protected void Aviso(string mensaje) => TempData["Aviso"] = mensaje;

    protected static string Fecha(DateOnly fecha) => fecha.ToString("yyyy-MM-dd");

    /// <summary>Arma la ruta con los parámetros no vacíos.</summary>
    protected static string Ruta(string baseRuta, params (string Nombre, object? Valor)[] parametros)
    {
        var consulta = new Dictionary<string, string?>();
        foreach (var (nombre, valor) in parametros)
        {
            var texto = valor switch
            {
                null => null,
                DateOnly d => Fecha(d),
                bool b => b ? "true" : "false",
                _ => valor.ToString()
            };
            if (!string.IsNullOrWhiteSpace(texto)) consulta[nombre] = texto;
        }
        return QueryHelpers.AddQueryString(baseRuta, consulta);
    }

    protected static string RutaCitas(string baseRuta, FiltroCitasDto f, params (string Nombre, object? Valor)[] extra) =>
        Ruta(baseRuta, new (string, object?)[]
        {
            ("desde", f.Desde), ("hasta", f.Hasta), ("idSede", f.IdSede), ("idMedico", f.IdMedico),
            ("idEspecialidad", f.IdEspecialidad), ("idEstadoCita", f.IdEstadoCita), ("idPaciente", f.IdPaciente)
        }.Concat(extra).ToArray());
}
