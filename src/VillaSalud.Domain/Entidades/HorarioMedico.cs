namespace VillaSalud.Domain.Entidades;

/// <summary>Bloque de atención de un médico: día de la semana, rango horario y consultorio.</summary>
public class HorarioMedico
{
    public int IdHorario { get; set; }
    public int IdMedico { get; set; }
    public int IdConsultorio { get; set; }
    /// <summary>0 = domingo ... 6 = sábado (igual que <see cref="DayOfWeek"/>).</summary>
    public byte DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public byte DuracionCitaMin { get; set; } = 30;
    public bool Vigente { get; set; } = true;

    public Medico? Medico { get; set; }
    public Consultorio? Consultorio { get; set; }

    public bool AplicaA(DateOnly fecha) => Vigente && (byte)fecha.DayOfWeek == DiaSemana;

    /// <summary>Indica si el rango [inicio, fin) cabe completo dentro del bloque.</summary>
    public bool Cubre(TimeOnly inicio, TimeOnly fin) => inicio >= HoraInicio && fin <= HoraFin && inicio < fin;

    public bool SeSolapaCon(byte diaSemana, TimeOnly inicio, TimeOnly fin) =>
        DiaSemana == diaSemana && HoraInicio < fin && inicio < HoraFin;

    /// <summary>Divide el bloque en intervalos de la duración configurada.</summary>
    public IEnumerable<(TimeOnly Inicio, TimeOnly Fin)> GenerarIntervalos()
    {
        var inicio = HoraInicio;
        while (true)
        {
            var fin = inicio.AddMinutes(DuracionCitaMin);
            // fin <= inicio ocurre si AddMinutes pasa de medianoche
            if (fin > HoraFin || fin <= inicio) yield break;
            yield return (inicio, fin);
            inicio = fin;
        }
    }
}
