using VillaSalud.Application.Abstracciones;

namespace VillaSalud.Infrastructure.Servicios;

/// <summary>Hora oficial de Perú (UTC-5, sin horario de verano), sin depender de la zona horaria del servidor.</summary>
public class RelojPeru : IReloj
{
    public DateTime Ahora => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-5), DateTimeKind.Unspecified);
}
