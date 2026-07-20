# Escenario D — Monitoreo Médico y Alertas Clínicas (Equipo D)

Sistema hospitalario distribuido con **.NET Aspire**, **RabbitMQ** y **PostgreSQL** que garantiza la entrega de alertas críticas incluso ante fallos de red o del broker.

## Arquitectura

```
Cliente 1 (Simulador) ──HTTP──► API 1 (Telemetría) ──► PostgreSQL (histórico + mensajes pendientes)
                                      │
                                      ▼
                                 RabbitMQ ──► API 2 (Alertas) ──SignalR──► Cliente 2 (Estación de Enfermería)
                                      │
                                      └──► DLQ (mensajes inválidos)
```

## Componentes

| Componente | Proyecto | Rol |
|---|---|---|
| Cliente 1 | `HospitalMonitoring.Client1.Simulator` | Simula dispositivos médicos enviando ráfagas de signos vitales |
| API 1 | `HospitalMonitoring.ApiService` | Recibe telemetría, detecta anomalías, encola alertas |
| API 2 | `HospitalMonitoring.Api2.Alerts` | Consume alertas, clasifica gravedad, empuja al dashboard |
| Cliente 2 | `HospitalMonitoring.Web` | Dashboard con alertas visuales y sonoras en tiempo real |

## Requisitos cumplidos

- **Orquestación .NET Aspire** con RabbitMQ y PostgreSQL
- **Mensajes pendientes**: si RabbitMQ no está disponible, API 1 guarda en tabla `PendingMessages` sin fallar
- **Worker de reintento**: reenvía en orden cronológico cuando el broker vuelve
- **Dead Letter Queue**: API 2 desvía mensajes corruptos o de negocio inválidos a `medical-alerts-dlq`

## Ejecución

```bash
dotnet run --project HospitalMonitoring.AppHost
```

Abre el **Aspire Dashboard** y navega a `client2-nursing-station` para ver la estación de enfermería.

## Endpoints API 1

- `POST /api/telemetry` — Recibir signos vitales
- `GET /api/telemetry/{patientId}` — Histórico del paciente
- `GET /api/pending-messages` — Ver mensajes pendientes de reenvío

## Pacientes válidos

`P-001`, `P-002`, `P-003`, `P-004`, `P-005`

## Demostración de resiliencia

1. Detén el contenedor RabbitMQ desde el Aspire Dashboard
2. Envía telemetría con anomalía → API 1 responde `queuedAsPending: true`
3. Verifica `GET /api/pending-messages`
4. Reinicia RabbitMQ → el worker reenvía automáticamente en orden cronológico

## Demostración de DLQ

Envía manualmente un mensaje con paciente inexistente o valores negativos; API 2 lo desvía a la cola `medical-alerts-dlq`.
