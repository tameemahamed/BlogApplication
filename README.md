# BlogApplication

A multi-author blog built on **ASP.NET Boilerplate** with an **Angular 19** single-page application and **PostgreSQL** (a local Supabase instance) as the database.

## Feature overview

- **Public reading** — anyone can browse the paginated feed of published posts (newest first, or most upvoted) and read a post by its slug. Unpublished content is never exposed on a public route or endpoint.
- **Post workflow** — authors write Markdown drafts, submit them for review, and a moderator or administrator approves, rejects (with a reason), or archives them. An approved post that is edited returns to review.
- **Markdown everywhere** — posts, comments, and replies are written in Markdown and rendered through `markdown-it` plus `DOMPurify`; raw HTML is never trusted.
- **Comments and replies** — one level of nesting; deeper replies are flattened with an `@author` reference so every comment renders in a single, readable thread.
- **Upvotes** — toggleable on posts, comments, and replies, with at most one row per user and target. Toggling off removes the row, so the unique index is a plain constraint.
- **Granular bans** — a moderator can ban a user from commenting, replying, and/or upvoting, in one action. Bans are ABP *user-level permission prohibitions*, so they are enforced by the same authorization layer as every other permission, with no separate enforcement code path. Each ban records a reason and full history in a ledger.
- **Moderation tools** — a moderation overview (banned users, recent comments, pending review queue) plus ban management from user management and directly from a comment.
- **Design system** — text-first UI: palette tokens only, no icons, emojis, or gradients; light and dark mode on every screen.

## Roles

| Role | Capabilities |
| --- | --- |
| **Admin** | Every permission, including post approval and ban management |
| **Moderator** | Post approval/archival, comment moderation, and ban management, plus authoring and community actions |
| **Author** | Create, edit, and delete their own posts and submit them for review, plus community actions |
| **User** | Comment, reply, and upvote (the default role assigned on registration) |

Only holders of `Pages.Blog.Posts.Approve` and `Pages.Blog.Bans.Manage` (Admin, Moderator) can approve posts and manage bans. Permissions are enforced server-side by the ABP permission system; hiding UI is cosmetic only.

## Solution layout

| Path | Description |
| --- | --- |
| `aspnet-core/src/BlogApplication.Core` | Domain entities (posts, comments, upvotes, bans), authorization, localization |
| `aspnet-core/src/BlogApplication.Application` | Application services and DTOs |
| `aspnet-core/src/BlogApplication.EntityFrameworkCore` | EF Core 9 mappings, migrations, host and demo seeders |
| `aspnet-core/src/BlogApplication.Web.Core` | Shared web logic (controllers, JWT bearer setup) |
| `aspnet-core/src/BlogApplication.Web.Host` | Executable API host (Kestrel, Swagger, log4net) |
| `aspnet-core/src/BlogApplication.Migrator` | Console app that applies migrations and seeds the database |
| `aspnet-core/test/BlogApplication.Tests` | xUnit integration tests |
| `angular/` | Angular 19 SPA: public site plus the author/moderator workspace |
| `supabase/` | Local Supabase project (PostgreSQL) |

## Getting started

Prerequisites: .NET 9 SDK, Node.js with npm (or yarn), the Supabase CLI, and the Angular CLI (`npm i -g @angular/cli`).

### 1. Database (Supabase)

```bash
cd supabase
supabase start
```

### 2. Backend

```bash
cd aspnet-core
dotnet restore
dotnet run --project src/BlogApplication.Migrator
dotnet run --project src/BlogApplication.Web.Host
```

Run the **Migrator** once (with Supabase running) before the first launch of `Web.Host`.

### 3. Frontend

```bash
cd angular
yarn install
npm start               # SPA on http://localhost:4200
```

### Regenerating the API clients

After adding or changing application services, run `Web.Host` and then:

```bash
cd angular
npm run nswag
```

`src/shared/service-proxies/service-proxies.ts` is generated — never hand-edit it. The provider list in `src/shared/service-proxies/service-proxy.module.ts` is *not* generated: each new `*ServiceProxy` class must be added there as well.

## Demo data

The Migrator seeds the following idempotently (safe to run repeatedly):

| Login | Password | Role |
| --- | --- | --- |
| `admin` | `123qwe` | Admin |
| `demo.moderator` | `123qwe` | Moderator |
| `demo.author` | `123qwe` | Author |
| `demo.member` | `123qwe` | User |
| `demo.banned` | `123qwe` | User, banned from commenting |

It also creates sample posts covering every workflow status, a comment thread with replies, upvotes on posts and a comment, and the ban ledger row together with its permission prohibition. A fresh environment (Supabase reset plus a Migrator run) therefore yields a fully browsable, seeded blog.

## Configuration

- Backend settings live in `appsettings.json` per project (JWT security key, `ConnectionStrings:Default`, `App:CorsOrigins`); user secrets are enabled.
- The frontend's API base URL is configured in `angular/src/assets/appconfig.json`.
