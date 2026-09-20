# LogiFlow

A logistics & order-fulfillment platform built to production patterns on **.NET 10**.
An order is placed, stock is reserved atomically, and a **transactional outbox**
reliably drives the order through confirmation and fulfillment — fully observable
via **OpenTelemetry**.

> Portfolio project. The goal is not feature breadth but a small, complete slice
> engineered the way a real backend service is: layered, tested, observable, and
> reproducible with one command.

## Stack

- **.NET 10**, ASP.NET Core Web API, **Blazor** (Server) UI
- **Clean Architecture** — Domain / Application / Infrastructure / API
- **CQRS** with MediatR; **FluentValidation** in the request pipeline
- **EF Core 10** + **SQL Server**
- **Transactional outbox** for reliable domain-event publishing
- **OpenTelemetry** — traces, metrics and logs (OTLP + console exporters)
- **Health checks** (liveness / readiness)
- **Docker Compose** (SQL Server + API + Web), **GitHub Actions** CI
- **xUnit** unit + integration tests (WebApplicationFactory)

## Architecture

```
LogiFlow.Domain          Entities, the order state machine, domain events. No dependencies.
   ▲
LogiFlow.Application      CQRS commands/queries + handlers, validation, DTOs, interfaces.
   ▲                      Depends only on Domain.
LogiFlow.Infrastructure   EF Core, the outbox (interceptor + background processor),
   ▲                      OpenTelemetry metrics. Depends on Application.
LogiFlow.Api              Controllers, OpenTelemetry wiring, health checks, Swagger.
LogiFlow.Web              Blazor UI; talks to the API over HTTP.
```

Dependencies point inward only — the Domain knows nothing about EF Core, HTTP, or MediatR's
implementation (it references `MediatR.Contracts`, interfaces only).

## The core flow

`POST /api/orders` → `PlaceOrderCommand`:

1. Load the referenced products.
2. `Order.Place(...)` creates a **Pending** order (raises `OrderPlacedEvent`).
3. Reserve stock line by line. If any line can't be satisfied, **all** reservations
   are rolled back and the order is **Rejected**; otherwise it is **Confirmed**.
4. `SaveChangesAsync` commits the order **and** the domain events (as outbox rows)
   in the **same transaction** — no dual-write between DB and message log.
5. The `OutboxProcessor` background service polls the outbox and publishes events.
   `OrderConfirmedEvent` triggers `FulfillOrderCommand`, which ships the reserved
   stock and marks the order **Fulfilled** (raising `OrderFulfilledEvent`).

State machine: `Pending → Confirmed → Fulfilled`, or `Pending → Rejected`.

## Why a transactional outbox?

Publishing an event and committing the state change are two different resources.
Do them separately and a crash between them either loses the event (change saved,
event never sent) or emits a phantom event (send succeeds, transaction rolls back).
The outbox removes the gap: the event is written to the same database, in the same
transaction, as the state change. A separate processor publishes it afterwards with
**at-least-once** delivery (handlers are written to be idempotent). See
`ConvertDomainEventsToOutboxInterceptor` and `OutboxProcessor`.

## Running it

### Docker Compose (everything)

```bash
docker compose up --build
```

- API + Swagger: http://localhost:5080/swagger
- Blazor UI: http://localhost:5090
- SQL Server: localhost,1433 (sa / Your_strong_Pass123)

The API applies EF migrations and seeds a product catalogue on startup.

### Local (SQL Server in Docker, apps via dotnet)

```bash
docker compose up -d sqlserver
dotnet run --project src/LogiFlow.Api      # http://localhost:5080
dotnet run --project src/LogiFlow.Web      # http://localhost:5090
```

## Tests

```bash
dotnet test
```

- **Unit** (`LogiFlow.UnitTests`): domain rules — reservation math and the order
  state machine.
- **Integration** (`LogiFlow.IntegrationTests`): command handlers over EF InMemory,
  and a full HTTP round-trip through `WebApplicationFactory` that places an order and
  asserts the outbox processor drives it to **Fulfilled**.

## API

| Method | Route | Purpose |
|--------|-------|---------|
| `GET`  | `/api/products`      | Catalogue with stock + available-to-promise |
| `POST` | `/api/orders`        | Place an order (reserves stock) |
| `GET`  | `/api/orders`        | List orders |
| `GET`  | `/api/orders/{id}`   | Get one order |
| `GET`  | `/health/live`       | Liveness |
| `GET`  | `/health/ready`      | Readiness (DB reachable) |

## Observability

Custom metrics via a `Meter` (`logiflow.orders.placed|confirmed|rejected|fulfilled`,
`logiflow.outbox.published`) plus ASP.NET Core / HttpClient auto-instrumentation.
Traces include a custom `outbox.publish` span per message. Set
`OTEL_EXPORTER_OTLP_ENDPOINT` to ship to a collector; otherwise console export is on
in Development.
