namespace VillaSalud.Domain.Excepciones;

/// <summary>Se incumple una regla de negocio (la API responde 400).</summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

/// <summary>La operación choca con el estado actual de los datos: cruce de horario o edición simultánea (409).</summary>
public class ConflictoException : Exception
{
    public ConflictoException(string mensaje) : base(mensaje) { }
}

/// <summary>El recurso solicitado no existe (404).</summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}

/// <summary>El usuario está autenticado pero no puede operar sobre ese recurso (403).</summary>
public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string mensaje) : base(mensaje) { }
}

/// <summary>Usuario o clave incorrectos, cuenta inactiva o bloqueada (401).</summary>
public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException(string mensaje) : base(mensaje) { }
}
