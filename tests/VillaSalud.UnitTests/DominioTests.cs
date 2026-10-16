using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;
using Xunit;

namespace VillaSalud.UnitTests;

public class DominioTests
{
    private static readonly DateTime Ahora = new(2026, 11, 9, 7, 0, 0);

    [Fact]
    public void Horario_divide_el_bloque_en_intervalos_de_la_duracion_configurada()
    {
        var horario = new HorarioMedico { HoraInicio = new(8, 0), HoraFin = new(10, 0), DuracionCitaMin = 30 };

        var intervalos = horario.GenerarIntervalos().ToList();

        Assert.Equal(4, intervalos.Count);
        Assert.Equal(new TimeOnly(8, 0), intervalos[0].Inicio);
        Assert.Equal(new TimeOnly(10, 0), intervalos[3].Fin);
    }

    [Fact]
    public void Horario_descarta_el_resto_que_no_alcanza_para_una_cita()
    {
        var horario = new HorarioMedico { HoraInicio = new(8, 0), HoraFin = new(9, 15), DuracionCitaMin = 30 };

        Assert.Equal(2, horario.GenerarIntervalos().Count());
    }

    [Fact]
    public void Horario_que_termina_cerca_de_medianoche_no_genera_intervalos_infinitos()
    {
        var horario = new HorarioMedico { HoraInicio = new(22, 0), HoraFin = new(23, 59), DuracionCitaMin = 45 };

        Assert.Equal(2, horario.GenerarIntervalos().Count());
    }

    [Theory]
    [InlineData(9, 0, 9, 30, true)]    // mismo intervalo
    [InlineData(9, 15, 9, 45, true)]   // se superpone parcialmente
    [InlineData(9, 30, 10, 0, false)]  // contiguo: empieza cuando termina la otra
    [InlineData(8, 30, 9, 0, false)]   // contiguo: termina cuando empieza la otra
    public void Cita_detecta_cruces_de_horario(int h1, int m1, int h2, int m2, bool esperado)
    {
        var cita = new Cita { Fecha = new(2026, 11, 9), HoraInicio = new(9, 0), HoraFin = new(9, 30) };

        Assert.Equal(esperado, cita.SeCruzaCon(new(2026, 11, 9), new(h1, m1), new(h2, m2)));
    }

    [Fact]
    public void RN08_cancelar_sin_motivo_es_rechazado()
    {
        var cita = new Cita();

        Assert.Throws<ReglaNegocioException>(() => cita.Cancelar("  ", 1, Ahora));
        Assert.Equal(EstadosCita.Pendiente, cita.IdEstadoCita);
    }

    [Fact]
    public void RN07_una_cita_atendida_no_se_puede_reprogramar_ni_cancelar()
    {
        var cita = new Cita();
        cita.MarcarAtendida("Sin novedades", 1, Ahora);

        Assert.Throws<ReglaNegocioException>(() => cita.Reprogramar(new(2026, 11, 10), new(9, 0), new(9, 30), 1, "Cambio", 1, Ahora));
        Assert.Throws<ReglaNegocioException>(() => cita.Cancelar("Ya no asistirá", 1, Ahora));
    }

    [Fact]
    public void Cada_cambio_de_estado_queda_en_el_historial()
    {
        var cita = new Cita();
        cita.RegistrarCreacion(7, Ahora);
        cita.Reprogramar(new(2026, 11, 10), new(9, 0), new(9, 30), 1, "A pedido del paciente", 7, Ahora);
        cita.Cancelar("El paciente viajó", 8, Ahora);

        Assert.Equal(3, cita.Historial.Count);
        Assert.Equal(EstadosCita.Reprogramada, cita.Historial[2].IdEstadoAnterior);
        Assert.Equal(EstadosCita.Cancelada, cita.Historial[2].IdEstadoNuevo);
        Assert.Equal(8, cita.Historial[2].IdUsuario);
    }

    [Fact]
    public void Usuario_se_bloquea_15_minutos_al_quinto_intento_fallido()
    {
        var usuario = new Usuario();

        for (var i = 0; i < 4; i++) usuario.RegistrarIntentoFallido(Ahora);
        Assert.False(usuario.EstaBloqueado(Ahora));

        usuario.RegistrarIntentoFallido(Ahora);
        Assert.True(usuario.EstaBloqueado(Ahora.AddMinutes(14)));
        Assert.False(usuario.EstaBloqueado(Ahora.AddMinutes(16)));
    }
}
