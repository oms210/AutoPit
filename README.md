# AutoPit – Vehicle Service System
*A Dealership Service Workflow built with .NET 9, React, RabbitMQ (6.6.0), Docker Compose & SQLite*

---

## 🚗 Overview

AutoPit is a complete end-to-end sample system representing a lightweight dealership service pipeline:

1. Front desk (React UI) adds vehicles and creates service requests.
2. API persists the request in SQLite and publishes an event to RabbitMQ.
3. A background worker consumes the message, simulates diagnostics, and writes a *ServiceOrder*.
4. The UI polls the API for request status and results.

This project showcases event-driven architecture, distributed workflow patterns, background processing, and modern .NET 9 development.

---

## 🏗️ Solution Structure

```
AutoPit/
│
├── src/
│   ├── AutoPit.Core/              # Domain models, interfaces, validation
│   ├── AutoPit.Infrastructure/    # RabbitMQ 6.6.0 bus, SQLite store, processor
│   ├── AutoPit.Api/               # .NET 9 Minimal API
│   ├── AutoPit.Worker/            # Background worker
│   └── AutoPit.Web/               # React + Vite + TypeScript UI
│
├── tests/
│   └── AutoPit.Tests/             # xUnit tests
│
├── docker-compose.yml
└── README.md
```

---

## 🧭 Architecture Diagram

```mermaid
flowchart LR
    A[React UI] -->|REST| B[AutoPit API (.NET 9)]
    B -->|CRUD| C[(SQLite DB)]
    B -->|Publish Request| D[[RabbitMQ Queue]]
    D -->|Consume| E[Worker (BackgroundService)]
    E -->|Write Order| C
    E -->|Update Status| C
```

---

## 🔄 Event Flow (Sequence)

```mermaid
sequenceDiagram
    participant UI as React UI
    participant API as .NET API
    participant MQ as RabbitMQ
    participant WK as Worker
    participant DB as SQLite

    UI->>API: POST /api/service
    API->>DB: Save ServiceRequest (Queued)
    API->>MQ: Publish message
    MQ-->>WK: Deliver message
    WK->>DB: Update status -> Diagnosing
    WK->>WK: Simulate diagnostics
    WK->>DB: Save ServiceOrder + Complete
    UI->>API: GET /api/service/{id}
    API->>DB: Fetch request + order
    API-->>UI: Return full status
```

---

## 📦 Components

### AutoPit.Core
- **Domain models:** `Car`, `ServiceRequest`, `ServiceOrder`
- **Interfaces:** `IAutoStore`, `IMessageBus`, `IServiceProcessor`
- **Validation:** Simple request and car validation helpers

### AutoPit.Infrastructure
- **SQLite** persistence with Dapper (WAL enabled; upserts for idempotency)
- **ServiceProcessor** simulates diagnostics and pricing
- **ChannelBus**: in-memory dev bus
- **RabbitMqBus (RabbitMQ.Client 6.6.0):** durable queue & direct exchange

**RabbitMQ 6.6.0 Notes (API surface):**
- Use `ConnectionFactory` + `CreateConnection()` to obtain `IConnection`
- Use `IConnection.CreateModel()` to obtain `IModel` (publisher/consumer)
- Use `AsyncEventingBasicConsumer` for async message handling
- Durable topology (exchange/queue) and persistent messages
- No reliance on `DispatchConsumersAsync` in this sample (works fine without setting it explicitly)

> If you previously saw compile errors like *“IModel not found”* or *“CreateConnection missing”*, ensure your package reference is exactly **RabbitMQ.Client 6.6.0**.

### AutoPit.Api
- Minimal API endpoints
  - `POST /api/cars`
  - `POST /api/service`
  - `GET  /api/service/{id}`
  - `GET  /api/service/inqueue`
- Swagger enabled (`/swagger`)
- CORS enabled for the React UI

### AutoPit.Worker
- `BackgroundService` that consumes from the bus
- Updates request status (`Queued` → `Diagnosing` → `Complete` / `Failed`)
- Persists `ServiceOrder` rows

### AutoPit.Web (React + Vite + TypeScript)
- Simple admin-style panel with four cards:
  - **Add Car** (create/update)
  - **Create Service Request**
  - **Check Status**
  - **View Queue**
- Uses `VITE_API_BASE` to call the API

---

## 🧪 Data Models (JSON)

### Car
```json
{
  "vin": "1HGCM82633A004352",
  "make": "Honda",
  "model": "Accord",
  "year": 2019,
  "trim": "EX"
}
```

### Service Request
```json
{
  "id": "2c78e4b6-8c93-4b92-8d0a-9a2752a27d7a",
  "vin": "1HGCM82633A004352",
  "concern": "Oil change & inspection",
  "priority": 3,
  "createdUtc": "2025-11-10T15:04:05.0000000Z",
  "status": "Queued"
}
```

### Service Order
```json
{
  "requestId": "2c78e4b6-8c93-4b92-8d0a-9a2752a27d7a",
  "technician": "Alex M",
  "findings": "Brake pad wear at 3mm; recommend replacement",
  "estimatedCost": 325.50,
  "completedUtc": "2025-11-10T15:08:35.0000000Z"
}
```

---

## 🔌 REST Endpoints

### `POST /api/cars`
Upsert a car.

### `POST /api/service`
Create a service request (status `Queued` and published to bus).

### `GET /api/service/{id}`
Fetch service request + order (if available).

### `GET /api/service/inqueue`
List queued requests.

---

## 🧰 Local Development (without Docker)

### API
```bash
dotnet run --project src/AutoPit.Api
# Swagger: http://localhost:5190/swagger
```

### Worker
```bash
dotnet run --project src/AutoPit.Worker
```

### React UI
```bash
cd src/AutoPit.Web
npm install
npm run dev
# http://localhost:5173
```
Set `VITE_API_BASE=http://localhost:5190` if needed (see `.env`).

> **RabbitMQ toggle**: set `USE_RABBITMQ=true` in API/Worker environment to switch from in-memory `ChannelBus` to RabbitMQ. RabbitMQ connection settings:
> `RabbitMQ__HostName`, `RabbitMQ__Port`, `RabbitMQ__UserName`, `RabbitMQ__Password`.

---

## 🐳 Running Everything with Docker Compose

From the repository root:
```bash
docker compose up --build
```

**Services**

| Service    | URL                           | Notes                     |
|------------|-------------------------------|---------------------------|
| Web (React)| http://localhost:5173         | Static SPA (Nginx)        |
| API        | http://localhost:5190         | Swagger at `/swagger`     |
| RabbitMQ   | http://localhost:15672        | Login: guest / guest      |
| Worker     | —                             | Background service        |

SQLite database file is stored in the `apidata` volume as `autopit.db`.

---

## 🐇 RabbitMQ Topology (6.6.0)

- **Exchange**: `autopit.exchange` (direct, durable)
- **Queue**: `autopit.service` (durable)
- **Routing Key**: `autopit.service`
- **Message**: serialized `ServiceRequest` (UTF‑8 JSON)
- **Publisher confirms**: not required for sample (could be added)
- **Persistent messages**: yes (`DeliveryMode = 2`)

Minimal pseudo-code used by the bus:

```csharp
var factory = new ConnectionFactory { HostName = host, Port = port, UserName = user, Password = pass };
using var connection = factory.CreateConnection();
using var pub = connection.CreateModel();
pub.ExchangeDeclare("autopit.exchange", ExchangeType.Direct, durable: true);
pub.QueueDeclare("autopit.service", durable: true, exclusive: false, autoDelete: false);
pub.QueueBind("autopit.service", "autopit.exchange", "autopit.service");

var props = pub.CreateBasicProperties();
props.DeliveryMode = 2; // persistent
pub.BasicPublish("autopit.exchange", "autopit.service", props, body);
```

Consumer (async) skeleton:

```csharp
var sub = connection.CreateModel();
sub.BasicQos(0, 16, false);

var consumer = new AsyncEventingBasicConsumer(sub);
consumer.Received += async (_, ea) =>
{
    try
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var req = JsonSerializer.Deserialize<ServiceRequest>(json)!;
        await channel.Writer.WriteAsync(req); // forward to in-process channel
        sub.BasicAck(ea.DeliveryTag, false);
    }
    catch
    {
        sub.BasicNack(ea.DeliveryTag, false, requeue: true);
    }
};

sub.BasicConsume(queue: "autopit.service", autoAck: false, consumer: consumer);
```

> Tested against **RabbitMQ.Client 6.6.0**. If you upgrade the client or server, re-check API changes.

---

## 🧪 Tests

```
dotnet test
```

- Integration-style test validates a request → process → order round-trip using the in-memory channel bus.

---

## 🧱 Non-Goals / Simplifications

- No auth (consider JWT/OIDC for production samples)
- No retries / DLQs (can be added via RabbitMQ policy or separate queues)
- No distributed tracing (can add OpenTelemetry later)
- SQLite used for demo purposes; replace with PostgreSQL/MySQL in real apps

---

## 🧭 Roadmap Ideas

- Technician assignment rules
- Schedules & SLAs
- Real VIN decoding (NHTSA API)
- Email/SMS notifications
- Dashboard visualizations in the React UI

---

## 📄 License

MIT
