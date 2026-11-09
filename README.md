# RaceDay – Part 3 (MVC Web App)

ASP.NET Core 8 MVC front end for the RaceDay event platform. It talks **only** to the Part 2 API
(no database or Azure access here); the API stores images in Azure Blob Storage.

## Roles
- **Organiser** – dashboard, create/edit/delete events (with banner upload), manage categories, view enrolments, capture & publish results.
- **Participant** – browse/filter upcoming events, view details, enter an event by category, see My Events (colour-coded status), My Results (finish time + position), edit profile and upload a profile picture.

## Run locally
1. Start the Part 2 API (note its URL, e.g. `https://localhost:7276/`).
2. Set `ApiSettings:BaseUrl` in `appsettings.json` to that URL.
3. `dotnet run` and open the URL printed in the console.

## Run with Docker (single command)
```
docker build -t raceday-web . && docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5000/ raceday-web
```
Open http://localhost:8080. Replace the API URL with wherever your API is reachable from the container
(on Linux add `--add-host=host.docker.internal:host-gateway`).

## API contract assumed (edit `Services/ApiRoutes.cs` and `Models/ApiModels.cs` if yours differs)
| Purpose | Call |
|---|---|
| Auth | `POST api/auth/register`, `POST api/auth/login` (returns `role`, `userID`), `POST api/auth/logout` |
| Profile | `GET/PUT api/profile`, `POST api/profile/picture` (multipart `file`) |
| Events | `GET/POST api/events`, `GET/PUT/DELETE api/events/{id}`, `POST api/events/{id}/banner` (multipart `file`) |
| Categories | `GET/POST api/events/{id}/categories`, `DELETE api/categories/{id}` |
| Enrolments | `POST api/enrolments {eventID, categoryID}`, `GET api/enrolments/my`, `GET api/events/{id}/enrolments` |
| Results | `GET/POST api/events/{id}/results`, `GET api/results/my` |

## Submission checklist (add yours)
- YouTube video: _add unlisted link_
- CI green build screenshot: _add screenshot_
- AI use disclosure: _state any AI assistance used, as required by the assignment_
