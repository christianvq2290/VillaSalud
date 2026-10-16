# Clínica Villa Salud – Plataforma de Gestión de Citas Médicas

Solución .NET 8 con API REST, cliente web MVC y base de datos SQL Server.

```
VillaSalud.sln
├─ src/
│  ├─ VillaSalud.Domain          Entidades y reglas (sin dependencias)
│  ├─ VillaSalud.Application     Casos de uso, DTO y contratos
│  ├─ VillaSalud.Infrastructure  EF Core (SQL Server), correo, PDF/Excel/CSV
│  ├─ VillaSalud.Api             API REST /api/v1 · JWT · Swagger · límites · auditoría
│  └─ VillaSalud.Web             Cliente web MVC (consume la API) + portal del paciente
├─ tests/
│  ├─ VillaSalud.UnitTests        Reglas de negocio y servicios
│  └─ VillaSalud.IntegrationTests API completa en memoria (HTTP real, sin SQL Server)
├─ bd/                            Scripts de base de datos
├─ pruebas/rendimiento/           Plan de JMeter
├─ docs/                          Asistente virtual (watsonx Assistant)
└─ .github/workflows/ci-cd.yml    Integración y despliegue continuo
```

## Requisitos

- Visual Studio 2022 (17.8 o superior) o el SDK de .NET 8
- SQL Server 2019 o superior (Express sirve) y SQL Server Management Studio

## Puesta en marcha

### 1. Base de datos

Elija **uno** de los dos scripts de la carpeta `bd/`:

| Situación | Script |
|---|---|
| Instalación nueva (no existe `BD_CitasMedicas`) | `01_creacion_bd.sql` – crea las 15 tablas y datos de ejemplo |
| Ya tiene `BD_CitasMedicas` con datos del modelo de 8 tablas | `02_migracion_desde_v1.sql` – conserva los datos (haga antes una copia de seguridad) |

### 2. Configuración de la API

La cadena de conexión de desarrollo está en `src/VillaSalud.Api/appsettings.Development.json`
(`Server=.\SQLEXPRESS`). Cámbiela si su instancia tiene otro nombre.

La clave de firma de los tokens **no** va en el repositorio. Regístrela una vez como secreto de usuario
(mínimo 32 caracteres):

```
cd src/VillaSalud.Api
dotnet user-secrets set "Jwt:Key" "escriba-aqui-una-frase-larga-de-al-menos-32-caracteres"
dotnet user-secrets set "Asistente:ApiKey" "clave-para-el-asistente-virtual"
```

(En Visual Studio: clic derecho en VillaSalud.Api > *Administrar secretos de usuario*.)
Sin `Jwt:Key` la API no arranca y lo indica en el mensaje de error.

### 3. Ejecutar

Deben ejecutarse **los dos** proyectos: primero la API y luego el cliente web.

- Visual Studio: clic derecho en la solución > *Configurar proyectos de inicio* > *Varios proyectos de inicio* >
  `VillaSalud.Api` = Iniciar y `VillaSalud.Web` = Iniciar.
- Consola:
  ```
  dotnet run --project src/VillaSalud.Api --launch-profile https     (https://localhost:7001/swagger)
  dotnet run --project src/VillaSalud.Web --launch-profile https     (https://localhost:7002)
  ```

Si el navegador advierte sobre el certificado: `dotnet dev-certs https --trust`.

### 4. Ingresar

| Usuario | Contraseña | Rol | Alcance |
|---|---|---|---|
| admin | Clave#2026 | Administrador | Todas las sedes |
| recepcion.ves | Clave#2026 | Recepcionista | Villa El Salvador |
| recepcion.sjm | Clave#2026 | Recepcionista | San Juan de Miraflores |
| erojas | Clave#2026 | Médico | Su agenda |

Si usó el script de migración, todos los usuarios existentes ingresan con `Temporal#2026`
y el sistema les pide cambiarla.

## Pruebas

```
dotnet test
```

- **Unitarias:** reglas RN01–RN10 (cruce de horarios, consentimiento, sedes, estados, concurrencia, bloqueo de cuenta…).
- **Integración:** levantan la API real con `WebApplicationFactory` y repositorios en memoria; verifican
  autenticación, autorización por rol, códigos HTTP y la generación de PDF, Excel y CSV.

### Rendimiento (JMeter 5.6)

Las pruebas de carga hacen muchos inicios de sesión desde una sola máquina; suba temporalmente el límite
en `appsettings.Development.json` de la API: `"Limites": { "LoginPorMinuto": 1000 }`.

```
cd pruebas/rendimiento
jmeter -n -t villasalud.jmx -Jhost=localhost -Jpuerto=7001 -Jusuarios=50 -Jrampa=30 -Jduracion=120 -l resultados/res.jtl -e -o resultados/informe
```

## Seguridad aplicada

- Contraseñas con hash PBKDF2 (sal por usuario); bloqueo de 15 minutos tras 5 intentos fallidos.
- JWT de 30 minutos + token de renovación de un solo uso; el cliente web guarda los tokens en una cookie cifrada `HttpOnly`.
- Autorización por rol y por sede en la API (no solo en las pantallas).
- Protección antifalsificación (CSRF) en todos los formularios; cabeceras de seguridad y HSTS.
- Límite de peticiones en el ingreso y en los servicios públicos.
- Consentimiento informado obligatorio antes de programar (Ley N.° 29733) y auditoría de operaciones.
- Secretos fuera del código: secretos de usuario en desarrollo, Azure Key Vault en producción (`KeyVault:Uri`).

## Correo de confirmación y recordatorio

Complete la sección `Smtp` (host, puerto, usuario, clave) con secretos de usuario. Si se deja vacía,
las notificaciones quedan en la tabla `NOTIFICACION` como pendientes y el resto del sistema funciona igual.

## Despliegue en Azure

1. Azure SQL Database: ejecute `bd/01_creacion_bd.sql` a partir de la sección `SEDE` (la base se crea desde el portal).
2. Dos App Service (.NET 8): uno para la API y otro para el cliente web.
3. Key Vault con los secretos `ConnectionStrings--VillaSalud`, `Jwt--Key`, `Asistente--ApiKey`, `Smtp--Clave`;
   active la identidad administrada del App Service de la API y dele permiso de lectura de secretos.
4. Configuración de la API: `KeyVault__Uri`, `Cors__OrigenesPermitidos__0` (URL del cliente web).
   Configuración del cliente web: `Api__UrlBase` (URL de la API).
5. El flujo `.github/workflows/ci-cd.yml` compila, prueba y despliega al hacer *push* a `main`
   (requiere los secretos y variables indicados al inicio del archivo).

## Asistente virtual

Vea `docs/asistente-watsonx.md`.

### Prueba de concurrencia (un solo intervalo)

```
jmeter -n -t villasalud.jmx -Jusuarios=0 -Jconcurrencia=20 -l resultados/concurrencia.jtl
```

Veinte usuarios envían a la vez la misma cita: el resultado correcto es **una** respuesta 201 y **diecinueve** 409.

## Monitoreo

Application Insights se activa desde el portal de Azure (App Service > Application Insights > Activar);
no requiere cambios en el código.
