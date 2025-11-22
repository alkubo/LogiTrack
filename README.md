# LogiTrack API

LogiTrack is a logistics order & inventory management Web API built with **.NET 10 / ASP.NET Core**, **EF Core (SQLite)**, **ASP.NET Identity + JWT**, and **in?memory caching** for performance.

## Key Features
- Inventory management: CRUD endpoints for `InventoryItem` (delete restricted by role `Manager`).
- Order management: `Order` with one?to?many relationship to `InventoryItem` and summary generation.
- Authentication & Authorization: User registration/login (JWT) + role based access control (`Manager`).
- Persistence: EF Core with migrations; automatic database migration & seeding via hosted service.
- Caching & Performance: `IMemoryCache` for inventory list and order detail; query timing headers (`X-Query-MS`), cache hit header (`X-Cache-Hit`).
- Resilience: Global exception handling middleware for consistent JSON error responses.

## Technology Stack
| Layer | Tech |
|-------|------|
| Language | C# 14 (.NET 10) |
| Framework | ASP.NET Core Web API |
| Data | EF Core + SQLite |
| Auth | ASP.NET Identity + JWT Bearer |
| Caching | IMemoryCache |
| Build | `dotnet` CLI |

## Architecture Overview
- `Models`: `InventoryItem`, `Order`, `ApplicationUser` encapsulate domain entities & business logic (`AddItem`, `RemoveItem`, `GetOrderSummary`).
- `LogiTrackContext`: Inherits `IdentityDbContext<ApplicationUser>`; configures one?to?many relationship and index on `CustomerName`.
- `Controllers`: `AuthController`, `InventoryController`, `OrderController` expose REST endpoints (async, role protected where needed).
- `StartupInitializer` hosted service applies migrations, creates Manager role & seed data.
- `Program.cs` wires services (Identity, JWT, DbContext, caching, exception middleware).

## Data Model
```
Order (1) --- (many) InventoryItem
Order: OrderId, CustomerName, DatePlaced, Items
InventoryItem: ItemId, Name, Quantity, Location, OrderId (nullable)
```
Delete behavior sets FK to null (retain orphaned inventory).

## Security
- Identity: Password hashing & user storage in SQLite tables.
- JWT: Bearer tokens returned from `/api/auth/login` (2 hour expiry demo).
- Roles: `Manager` role seeded; destructive endpoints use `[Authorize(Roles="Manager")]`.
- Protected Controllers: `[Authorize]` on Inventory & Orders.
- Input validation: DTO property checks; standardized `ProblemDetails` responses.

## Caching & Performance
- `IMemoryCache` for: Inventory list (`inventory_all`, 30s TTL) & individual order (`order_{id}`, 30s TTL).
- Invalidation on POST/DELETE operations.
- Query optimizations: `AsNoTracking()` on read paths; selective `Include` only where items required; projection for order summary list.
- Index on `Orders.CustomerName` improves filtering/search potential.
- Headers: `X-Cache-Hit`, `X-Query-MS` enable lightweight timing diagnostics.

## Setup & Run
```bash
dotnet restore
dotnet build
dotnet run --project LogiTrack.Api
```
Hosted service applies migrations & seeds:
- Role: Manager
- User: manager@logitrack.local / Pass@word1!
- Inventory item: Pallet Jack (quantity 12)

## Example Requests
Register:
```http
POST /api/auth/register
Content-Type: application/json
{"email":"user@example.com","password":"Pass@word1!"}
```
Login (get JWT):
```http
POST /api/auth/login
{"email":"user@example.com","password":"Pass@word1!"}
```
Get Inventory (check caching headers):
```http
GET /api/inventory
Authorization: Bearer <token>
```
Create Order:
```http
POST /api/orders
Authorization: Bearer <token>
{
  "customerName": "Acme",
  "items": [
    {"name":"Pallet Jack","quantity":1,"location":"Warehouse A"},
    {"name":"Hand Truck","quantity":2,"location":"Warehouse B"}
  ]
}
```
Delete Inventory (Manager role):
```http
DELETE /api/inventory/1
Authorization: Bearer <manager_token>
```

## OpenAPI / Swagger
In Development: JSON spec at `/openapi/v1.json`.

## Migration Management
```bash
dotnet ef migrations add <Name> --project LogiTrack.Api
dotnet ef database update --project LogiTrack.Api
```

## Development Challenges & Solutions
| Challenge | Resolution |
|-----------|-----------|
| `EnsureCreated` vs migrations conflict | Replaced with `Database.Migrate()` & removed old DB file |
| Missing JWT namespaces | Added JwtBearer & System.IdentityModel.Tokens.Jwt packages |
| PK recognition issues | Added `[Key]` attributes & refined model configuration |
| Repeated/unoptimized queries | Added caching, `AsNoTracking`, projections, indexing |

## Testing Checklist
- Auth: Register + login returns JWT.
- Role enforcement: Delete endpoints restricted.
- Caching: Second GET shows `X-Cache-Hit: true`.
- Persistence: Data survives process restart.
- Performance: Timing headers reflect improvements.

## Extensibility Ideas
- Pagination & filtering.
- Distributed cache (Redis) & output caching.
- Refresh tokens & stronger password policies.
- Rate limiting & structured logging (Serilog).
- ETag / conditional GET support.
- Soft deletes & optimistic concurrency tokens.

## License
Add your chosen license (e.g., MIT) here.

---
**Status:** Capstone completed (Parts 1–5).