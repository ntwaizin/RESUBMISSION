# RaceDay – Part 2: RESTful API

![RaceDay CI]([https://github.com/ntwaizin/PROG_P2/actions/workflows/ci.yml/badge.svg)](https://github.com/ntwaizin/RESUBMISSION/actions)

RaceDay is an event management system for South African road running, walking and
cycling events. This repository contains the **Part 2 REST API** (ASP.NET Core 8,
Entity Framework Core Code-First, SQL Server, server-side sessions, Swagger and xUnit tests).

| Item | Link |
|---|---|
 |YOUTUBE LINK https://youtu.be/OMAWLUNDShA
| GitHub Actions | [https://github.com/ntwaizin/PROG_P2/actions/workflows/ci.yml/badge.svg)](https://github.com/ntwaizin/RESUBMISSION/actions |

## Roles

| Role | What they can do |
|---|---|
| **Organiser** | Create, edit and delete their own events; add, edit and delete categories; view all enrolments for their events; capture and update results. |
| **Participant** | Register and log in; browse events and categories; enrol in an event by choosing a category; view/cancel their own enrolments; view their own results; manage their profile. |

Both roles can view and update their own profile. Browsing events, categories and results is public.

## How authentication works
1. `POST /api/auth/register` – the user chooses the role (`Organiser` or `Participant`). The password is hashed with ASP.NET Core Identity's `PasswordHasher`; the plain password is never stored.
2. `POST /api/auth/login` – on success the **server-side session** stores `AccountID`, `UserID` (OrganiserID or ParticipantID) and `Role`.
3. Every protected endpoint uses `[SessionAuthorize(...)]`: no session → **401**, wrong role → **403**. Organisers can only change their *own* events, categories and results.
4. `POST /api/auth/logout` clears the session.

## Endpoints

| Method | Route | Role | Description |
|---|---|---|---|
| POST | /api/auth/register | Public | Register as Organiser or Participant |
| POST | /api/auth/login | Public | Log in (starts session) |
| POST | /api/auth/logout | Public | Log out (clears session) |
| GET | /api/profile | Any logged in | View own profile |
| PUT | /api/profile | Any logged in | Update own profile |
| GET | /api/events | Public | List events (optional `?eventType=Run`, `Walk` or `Cycle`) |
| GET | /api/events/{id} | Public | Event details with categories |
| POST | /api/events | Organiser | Create event |
| PUT | /api/events/{id} | Organiser (owner) | Update event |
| DELETE | /api/events/{id} | Organiser (owner) | Delete event (blocked if it has enrolments) |
| GET | /api/events/{eventId}/categories | Public | List categories for an event |
| POST | /api/events/{eventId}/categories | Organiser (owner) | Add a category |
| GET | /api/categories/{id} | Public | View one category |
| PUT | /api/categories/{id} | Organiser (owner) | Update category |
| DELETE | /api/categories/{id} | Organiser (owner) | Delete category (blocked if it has enrolments) |
| POST | /api/enrolments | Participant | Enrol in a category |
| GET | /api/enrolments | Participant | View own enrolments |
| GET | /api/enrolments/{id} | Participant (owner) / Organiser (event owner) | View one enrolment |
| DELETE | /api/enrolments/{id} | Participant (owner) | Cancel enrolment |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) | View everyone enrolled in an event |
| GET | /api/results | Public | List results |
| GET | /api/results/mine | Participant | View own results |
| GET | /api/results/{id} | Public | View one result |
| POST | /api/results | Organiser (owner) | Record a result |
| PUT | /api/results/{id} | Organiser (owner) | Update a result |

All endpoints are visible and testable in **Swagger** (`/swagger`) with descriptions and response codes.

## Changes from the Part 1 plan / ERD (and why)
| Change | Reason |
|---|---|
| New **Accounts** table (Email, PasswordHash, Role, OrganiserID / ParticipantID) | Part 2 needs hashed passwords, login and roles. Keeps Organisers/Participants as in the ERD. |
| `Events.DistanceKM`, `Events.EventType` (Run/Walk/Cycle) | Required by the Part 2 brief. |
| `Events.BannerUrl` (nullable) | Added now for Azure Blob Storage in Part 3. |
| `POST /api/auth/logout`, `GET /api/results/mine`, `GET /api/categories/{id}`, `GET /api/enrolments/{id}` | Extra endpoints needed to end sessions and let participants view their own results. |
| Category routes: create/list under `/api/events/{eventId}/categories` | Matches the Part 1 plan. |
| `ResultStatus` values: Finished, DNF, DNS, Pending | Matches the Part 1 seed data. |
| A participant can enrol **once per event**; events/categories with enrolments cannot be deleted | Prevents duplicate entries and silent loss of enrolment/result data. |

## Setup and run
**Requirements:** .NET 8 SDK, SQL Server (LocalDB comes with Visual Studio), Visual Studio 2022 or VS Code.

```bash
git clone https://github.com/ntwaizin/PROG_P2.git
cd PROG_P2
```
1. Check the connection string in `appsettings.json` (`RaceDayConnection`). The default uses LocalDB.
2. Create the database (first time only):
   ```bash
   dotnet tool install --global dotnet-ef      # once
   dotnet ef database update
   ```
   (The API also applies migrations automatically when it starts.)
3. Run the API:
   ```bash
   dotnet run
   ```
4. Open `https://localhost:<port>/swagger` (the port is shown in the console / `launchSettings.json`).
5. In Swagger: **register** → **login** → try the other endpoints. The session cookie is kept automatically.

## Running the tests
```bash
dotnet test PROG_P2.sln
```
The tests start the real API in memory (EF Core InMemory database), so SQL Server is not needed.
They cover, for both success **and** failure cases:
* **Auth** – register (both roles), duplicate email, invalid role, weak password, hashed password stored, login success/failure, logout clears the session, profile view/update, 401 without login.
* **Events** – organiser create/update/delete, validation errors, 401 when logged out, 403 for participants and for other organisers, public viewing and filtering.
* **Role enforcement** – participants rejected from organiser endpoints, organisers rejected from participant endpoints, protected endpoints return 401 when anonymous.
* **Enrolments** – participant enrols (201) and the participant/event/category link is stored, duplicates (409), unknown category (404), own-enrolment list, organiser sees event enrolments, cancel.
* **Results and categories** – organiser records/updates results, participants see only their own, validation and ownership checks.

## CI/CD
GitHub Actions (`.github/workflows/ci.yml`) runs on every push: validates the repository structure, builds the solution and runs all tests.

**Green build screenshot:**

![Successful CI build](docs/ci-green-build.png)

## Project structure
```
PROG_P2/
├── .github/workflows/ci.yml   CI workflow (build + tests)
├── Controllers/               API endpoints
├── Data/RaceDayContext.cs     EF Core context, keys, relationships, constraints
├── DTOs/                      Request/response models (no entities exposed)
├── Helpers/                   Session helper + [SessionAuthorize] role filter
├── Models/                    EF Core entities (match the ERD)
├── PROG_P2.Tests/             xUnit tests
├── Program.cs                 Startup: EF Core, session, Swagger
└── README.md
```

RaceDay – Part 3 (MVC Web App)

ASP.NET Core 8 MVC front end for the RaceDay event platform. It talks **only** to the Part 2 API
(no database or Azure access here); the API stores images in Azure Blob Storage.
