# Asistente virtual del portal del paciente (IBM watsonx Assistant)

El portal público (`/Inicio/Portal` del cliente web) incluye el web chat de watsonx Assistant.
El asistente responde preguntas frecuentes y consulta datos reales de la clínica a través de
tres servicios de solo lectura de la API (`/api/v1/publico/...`).

## 1. Crear el asistente

1. En IBM Cloud cree una instancia de **watsonx Assistant** (plan Lite) y un asistente llamado *Villa Salud*.
2. **Integrations > Extensions > Build custom extension**: suba `docs/asistente-openapi.json`.
3. Al agregar la extensión al asistente:
   - *Authentication type*: **API key auth** – valor: la misma clave configurada en la API como `Asistente:ApiKey`.
   - *Server variable host*: el host público de la API (por ejemplo `villasalud-api.azurewebsites.net`).
     watsonx Assistant llama a la API desde internet: no puede alcanzar `localhost`.

## 2. Acciones sugeridas

| Acción | El paciente dice… | Paso |
|---|---|---|
| Sedes y direcciones | "¿dónde están ubicados?" | Usar extensión `listarSedes` y mostrar nombre, dirección y distrito |
| Especialidades | "¿qué especialidades tienen?", "¿atiende cardiología?" | Usar extensión `listarEspecialidades` |
| Estado de mi cita | "quiero saber el estado de mi cita" | Pedir DNI y código de cita → `consultarEstadoCita` → si responde 404: "No encontré una cita con esos datos" |
| Cómo reservar | "quiero sacar una cita" | Respuesta fija: la reserva se hace en recepción o por teléfono; mostrar teléfono y horario |
| Horario de atención | "¿a qué hora atienden?" | Respuesta fija |
| Hablar con una persona | "quiero hablar con alguien" | Respuesta fija con el teléfono de la sede |

El asistente **no** programa ni cancela citas y **no** muestra el motivo de consulta ni datos clínicos:
el servicio de estado devuelve solo fecha, hora, médico, consultorio, sede y estado.

## 3. Conectar el web chat al portal

En **Integrations > Web chat > Embed** copie `integrationID`, `region` y `serviceInstanceID`
y colóquelos en `src/VillaSalud.Web/appsettings.json`:

```json
"Asistente": { "IntegrationId": "…", "Region": "us-south", "ServiceInstanceId": "…" }
```

Mientras esos valores estén vacíos, el portal muestra las preguntas frecuentes sin el chat.
