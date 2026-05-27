# Bloomdo

A client-server Android application for screen-time management, habit building,
and distraction blocking. Bloomdo helps users reduce smartphone overuse by combining
app-blocking rules, daily activity tracking with streaks, and an AI assistant
powered by Google Gemini.

> Diploma project. .NET 10 + ASP.NET Core on the server, Avalonia UI on the client.

---

## Table of contents

- [Overview](#overview)
- [Features](#features)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Project structure](#project-structure)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [REST API](#rest-api)
- [Security](#security)
- [Database](#database)
- [Roadmap](#roadmap)

---

## Overview

Bloomdo addresses the problem of compulsive smartphone use and lack of a single
tool that ties together habit tracking and distraction blocking. The app gives
the user four levers:

- Track per-app screen time on the device.
- Block apps by schedule, daily limit, focus session, or until daily habits are done.
- Maintain daily habits / tasks with streak counting.
- Get personalised productivity advice from an AI assistant that has access to
  the user's real screen-time and habit data.

Monetisation is built in via Stripe (Free vs Plus plans).

---

## Features

### App blocking (four rule types)

| Type     | Trigger                                                                 |
| -------- | ----------------------------------------------------------------------- |
| Schedule | Active on selected weekdays inside a time window                        |
| Limit    | Active after the user exceeds a daily usage cap (minutes)               |
| Focus    | Active for the duration of a manually started focus session             |
| Bloomdo  | Active until the user completes every task in a linked activity group   |

Enforcement runs as an Android Foreground Service polling the foreground app
every 2 seconds via `UsageStatsManager.QueryEvents`. When a rule matches, a
full-screen `BlockedActivity` is launched on top of the blocked app
(`SYSTEM_ALERT_WINDOW` permission).

### Daily activities

- Hierarchical structure: groups -> tasks.
- Four task types: Timer, Count, Steps, Checkbox.
- Per-task and per-group streak tracking.
- Optional link to the Bloomdo block type — apps unblock once the linked group
  is complete.

### Statistics

- Per-app usage collected via `UsageStatsManager`, system apps filtered out.
- Daily aggregates: total screen time, pickups, goal-met flag.
- Calendar view with streaks and StreakFreeze for missed days.
- Weekly analytics with week-over-week change percentages.
- Automatic streak-freeze application for Plus users.

### AI chat (Gemini 2.5 Flash)

- Context-aware assistant — system prompt is rebuilt from the user's real data:
  weekly screen time, tasks and their status, active block rules.
- Multi-turn conversations with history (up to 50 messages).
- Load balancing across multiple Gemini API keys.
- Free-tier limit: 10 messages per day.

### Subscription (Freemium + Stripe)

| Tier   | AI / day  | Block rules | Customisation | Weekly stats | Streak freezes / month |
| ------ | --------- | ----------- | ------------- | ------------ | ---------------------- |
| Free   | 10        | 3           | No            | No           | 0                      |
| Plus   | Unlimited | Unlimited   | Yes           | Yes          | 2                      |

Full Stripe flow: customer creation, Checkout Session, webhooks
(`checkout.session.completed`, `customer.subscription.updated/deleted`,
`invoice.payment_succeeded/failed`), Success / Cancel HTML pages.

---

## Tech stack

| Layer            | Technology                                                  |
| ---------------- | ----------------------------------------------------------- |
| Language         | C# 14, .NET 10                                              |
| Server           | ASP.NET Core 10 Web API                                     |
| Client UI        | Avalonia UI 11.3 (cross-platform XAML)                      |
| Android host     | Avalonia.Android                                            |
| MVVM             | CommunityToolkit.Mvvm 8.4 (source generators)               |
| UI components    | ShadUI (custom, local submodule)                            |
| Server DB        | PostgreSQL + Npgsql.EntityFrameworkCore.PostgreSQL 10       |
| Client DB        | SQLite + Microsoft.EntityFrameworkCore.Sqlite 10            |
| ORM              | EF Core 10 (Code First, migrations)                         |
| Auth             | JWT Bearer + refresh-token rotation                         |
| Password hashing | BCrypt.Net-Next                                             |
| Validation       | FluentValidation 12                                         |
| AI               | Google.GenAI 1.5 (Gemini 2.5 Flash)                         |
| Payments         | Stripe.net 50 (Checkout, Subscriptions, Webhooks)           |
| Logging          | Serilog (Console + rolling File)                            |
| API docs         | Swashbuckle.AspNetCore (Swagger / OpenAPI)                  |
| Package mgmt     | Central Package Management (`Directory.Packages.props`)     |
| Platform         | Microsoft.Maui.Essentials (SecureStorage, Preferences)      |

---

## Architecture

Clean Architecture inside each side of the system.

```
+---------------------------------------------------------------+
|                       Bloomdo solution                        |
|                                                               |
|   +-----------------+         REST / JSON          +--------+ |
|   |     Client      | <---------------------------> | Server | |
|   | (Avalonia + EF  |   JWT Bearer + Refresh        | (ASP   | |
|   |  SQLite local)  |                               |  .NET) | |
|   +--------+--------+                               +---+----+ |
|            |                                            |      |
|            v                                            v      |
|   Android Foreground Service                       PostgreSQL  |
|   - UsageStatsManager                              + EF Core   |
|   - BlockedActivity overlay                                    |
|                                                                |
+----------------------------------------------------------------+

                          Bloomdo.Shared
                 DTOs / Enums / ApiRoutes / Permissions
```

### Server layers

```
Api  ->  Application  ->  Infrastructure  ->  Domain
                   \____________ Domain has zero dependencies
```

- **Domain** — entities (`Account`, `BlockRule`, `ActivityGroup`,
  `ActivityItem`, `ActivityCompletion`, `ChatConversation`, `ChatMessage`,
  `Subscription`, `StreakFreeze`, `DailySnapshot`, `AppUsageRecord`,
  `Achievement`), `BaseEntity` with soft-delete + audit, domain exceptions.
- **Application** — service interfaces and business logic
  (`AuthService`, `BlockService`, `StatsService`, `DailyActivityService`,
  `ChatService`, `SubscriptionService`, `AchievementService`).
- **Infrastructure** — `AppDbContext`, generic `Repository<T>` + specialised
  repositories, JWT service, EF migrations, `DevDataSeeder`.
- **Api** — controllers, `ExceptionHandlingMiddleware`, permission-based
  authorization (`PermissionAuthorizationHandler`,
  `PermissionPolicyProvider`, `[RequirePermission]`).

### Client layers

- **Core** — interfaces for every client service (25+).
- **Domain** — client models (`AppUsageInfo`, `InstalledAppInfo`),
  `[Authorize]` attribute for ViewModel-first nav.
- **Application** — 35+ ViewModels, `NavigationService`, `AuthorizationService`.
- **Infrastructure** — HTTP API clients, `AccessTokenManager`,
  `AuthHeaderHandler` (proactive + reactive token refresh),
  `TokenStorage` (SecureStorage), local stores, `LocalDatabaseContext` (SQLite).
- **UI** — Avalonia views, `ViewLocator`, converters, custom `SwipeRevealPanel`,
  toast / dialog / timer / theme services, `MarkdownHelper`.
- **Android** — `MainActivity`, `AndroidAppUsageService`,
  `AndroidInstalledAppsService`, `AndroidAppIconProvider`,
  `AndroidBlockEnforcementService`, `BlockEnforcementForegroundService`.

---

## Project structure

```
src/
  Bloomdo.Shared/                # DTOs, Enums, ApiRoutes, Permissions
  Bloomdo.Server/
    Bloomdo.Server.Api/          # ASP.NET Core entry point, controllers, middleware
    Bloomdo.Server.Application/  # Services, validators, interfaces
    Bloomdo.Server.Infrastructure/
                                 # EF Core, DbContext, JWT, Stripe, Gemini
    Bloomdo.Server.Domain/       # Entities, business rules
  Bloomdo.Client/
    Bloomdo.Client.Startup/      # Desktop entry, DI container
    Bloomdo.Client.Android/      # Android entry, MainActivity, platform services
    Bloomdo.Client.UI/           # Avalonia views, controls, converters
    Bloomdo.Client.Application/  # ViewModels, app services
    Bloomdo.Client.Infrastructure/
                                 # API clients, SQLite, token management
    Bloomdo.Client.Core/         # Service interfaces
    Bloomdo.Client.Domain/       # Client models, enums
Directory.Packages.props         # Central NuGet version management
```

15 projects total: 4 server, 6 client (incl. Android), 1 shared, plus
startup / entry projects.

---

## Getting started

### Prerequisites

- .NET 10 SDK
- PostgreSQL 14+
- Android SDK / Android Studio (for the mobile target)
- Optional: Stripe and Google Gemini API keys for full functionality

### Server

```bash
# Restore and build the whole solution
dotnet build

# Apply database migrations
dotnet ef database update \
  --project src/Bloomdo.Server/Bloomdo.Server.Infrastructure \
  --startup-project src/Bloomdo.Server/Bloomdo.Server.Api

# Run the API
dotnet run --project src/Bloomdo.Server/Bloomdo.Server.Api/Bloomdo.Server.Api.csproj
```

Swagger UI is available in `Development` mode at `https://localhost:7270/swagger`.

### Client — Desktop

```bash
dotnet run \
  --project src/Bloomdo.Client/Bloomdo.Client.Startup/Bloomdo.Client.Startup.csproj
```

### Client — Android

```bash
dotnet run \
  --project src/Bloomdo.Client/Bloomdo.Client.Android/Bloomdo.Client.Android.csproj \
  -f net10.0-android
```

The Android emulator reaches the host machine via `10.0.2.2`. In `DEBUG`
builds SSL certificate validation is bypassed so self-signed dev certs work.

### Adding a new EF migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Bloomdo.Server/Bloomdo.Server.Infrastructure \
  --startup-project src/Bloomdo.Server/Bloomdo.Server.Api
```

> NuGet versions are centralised in `Directory.Packages.props`. Do **not**
> add `Version=` to individual `.csproj` files.

---

## Configuration

Use .NET User Secrets in development:

```bash
cd src/Bloomdo.Server/Bloomdo.Server.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=bloomdo;Username=...;Password=..."
dotnet user-secrets set "JwtSettings:Secret" "<random 64-byte secret>"
dotnet user-secrets set "GeminiSettings:ApiKeys:0" "<gemini-key-1>"
dotnet user-secrets set "GeminiSettings:ApiKeys:1" "<gemini-key-2>"
dotnet user-secrets set "StripeSettings:SecretKey" "sk_test_..."
dotnet user-secrets set "StripeSettings:WebhookSecret" "whsec_..."
```

### Free-tier limits (server-side enforcement)

| Setting                | Value |
| ---------------------- | ----- |
| AI messages per day    | 10    |
| Active block rules     | 3     |
| Streak freezes / month | 0     |

---

## REST API

20+ endpoints, all routes centralised in `ApiRoutes`:

| Group                | Endpoints                                                            |
| -------------------- | -------------------------------------------------------------------- |
| `api/auth/*`         | Register, Login, Refresh, Revoke, Me, UpdateProfile, ProfileStats    |
| `api/blocks/*`       | List / Create / Update / Delete block rules                          |
| `api/stats/*`        | Sync, Daily, Calendar, Weekly                                        |
| `api/activities/*`   | Daily, Groups CRUD, Items CRUD, Toggle completion                    |
| `api/chat/*`         | Conversations list / detail / delete, Send message                   |
| `api/subscription/*` | Status, Checkout, Cancel, Webhook, CheckoutSuccess / Cancel          |
| `api/achievements/*` | List                                                                 |

All responses use DTOs from `Bloomdo.Shared`. The full schema is published via
Swagger / OpenAPI.

---

## Security

- **JWT Bearer** with access + refresh token flow. Refresh tokens are rotated
  on every use (old revoked, new issued). 30-second grace period for
  duplicate concurrent refresh requests. Refresh tokens stored server-side with
  client IP and replacement history.
- **Access tokens** carry `sub` (accountId), `Email`, roles, and a custom
  `perm` claim listing granular permissions.
- **Client token storage** via `SecureStorage` (Microsoft.Maui.Essentials).
- **Permission model** — 16+ granular permissions
  (`profile:view`, `blocks:manage`, `chat:access`, `premium:access`,
  `roles:manage`, ...). Enforced server-side by
  `IAuthorizationPolicyProvider` + `PermissionAuthorizationHandler`, and
  client-side by `AuthorizationService` before navigation.
- **Passwords** — BCrypt with automatic salt.
- **Error mapping** — domain exceptions mapped to correct HTTP status codes
  by `ExceptionHandlingMiddleware`
  (`InvalidCredentialsException` -> 401, `EmailAlreadyExistsException` -> 409,
  `ForbiddenAccessException` -> 403, etc.).
- **Stripe webhooks** verified via the `Stripe-Signature` header.
- **Rate limits in business logic** — `ChatLimitExceededException`,
  `BlockLimitExceededException`.
- **Dev mode only** — SSL validation is bypassed, Kestrel binds to
  `ListenAnyIP`. Production builds enforce TLS.

---

## Database

- **Server (PostgreSQL)** — 17 tables (`Accounts`, `AccountRoles`, `Roles`,
  `RolePermissions`, `RefreshTokens`, `AppUsageRecords`, `DailySnapshots`,
  `BlockRules`, `Achievements`, `AccountAchievements`, `ActivityGroups`,
  `ActivityItems`, `ActivityCompletions`, `ChatConversations`, `ChatMessages`,
  `Subscriptions`, `StreakFreezes`).
- All entities inherit `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`,
  `IsDeleted`, `DeletedAt`, audit `CreatedBy`/`UpdatedBy`/`DeletedBy`).
- **Soft delete** via `IsDeleted` + EF global query filters.
- **UUIDs** generated by PostgreSQL `uuid-ossp` extension
  (`uuid_generate_v4()`).
- **jsonb columns** for `BlockedPackagesJson`, `ScheduleDaysJson`.
- **Unique indexes** with `IsDeleted = false` filter.
- 4 migrations: `Initial`, `Add-AI-Gemini`, `Stripe_payment`, `Freeze`.

- **Client (SQLite)** — `LocalDatabaseContext`, stored at
  `{AppDataDirectory}/BloomdoLocal.db`, migrations applied on startup.

---

## Roadmap

- iOS target (Avalonia.iOS).
- Push notifications for habit reminders.
- Group activities (share habit lists with friends).
- More AI personas / coaching modes.
- Wear OS companion for quick habit check-ins.

---

## License

TBD — diploma project, all rights reserved until a license is chosen.

---

## Author

Vladyslav Kozhuhivskyi — Computer Science student, .NET developer.
Diploma project, 2026.
