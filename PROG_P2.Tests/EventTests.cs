using System.Net;
using System.Net.Http.Json;
using PROG_P2.DTOs;

namespace PROG_P2.Tests
{
    /// <summary>Event management: organisers can manage events, everyone else is rejected.</summary>
    public class EventTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public EventTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        private static object ValidEvent(string eventType = "Run") => new
        {
            eventName = "Joburg 10km",
            eventDate = DateTime.UtcNow.AddDays(20),
            location = "Johannesburg",
            description = "Test",
            distanceKM = 10.0,
            eventType
        };

        [Fact]
        public async Task GetEvents_WithoutLogin_Returns200()
        {
            var response = await _factory.NewClient().GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201AndStoresTheEvent()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent("Cycle"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<EventResponseDTO>();
            Assert.Equal("Cycle", created!.EventType);
            Assert.Equal(await organiser.UserIdAsync(), created.OrganiserID);

            var fetched = await _factory.NewClient().GetAsync($"/api/events/{created.EventID}");
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithoutLogin_Returns401()
        {
            var response = await _factory.NewClient().PostAsJsonAsync("/api/events", ValidEvent());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_Returns403()
        {
            var participant = await _factory.LoggedInClientAsync("Participant");

            var response = await participant.PostAsJsonAsync("/api/events", ValidEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithInvalidType_Returns400()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent("Swimming"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithMissingName_Returns400()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", new
            {
                eventDate = DateTime.UtcNow.AddDays(5),
                location = "Cape Town",
                distanceKM = 5.0,
                eventType = "Walk"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetEvent_ThatDoesNotExist_Returns404()
        {
            var response = await _factory.NewClient().GetAsync("/api/events/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetEvent_IncludesItsCategories()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();
            await organiser.CreateCategoryAsync(ev.EventID);

            var fetched = await _factory.NewClient().GetFromJsonAsync<EventResponseDTO>($"/api/events/{ev.EventID}");

            Assert.Single(fetched!.Categories);
        }

        [Fact]
        public async Task GetEvents_CanFilterByType()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            await organiser.CreateEventAsync("Cycle");

            var cycles = await _factory.NewClient().GetFromJsonAsync<List<EventResponseDTO>>("/api/events?eventType=Cycle");

            Assert.NotEmpty(cycles!);
            Assert.All(cycles!, e => Assert.Equal("Cycle", e.EventType));
        }

        [Fact]
        public async Task UpdateEvent_AsOwner_Returns200()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();

            var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventID}", new
            {
                eventName = "Renamed Event",
                eventDate = DateTime.UtcNow.AddDays(40),
                location = "Pretoria",
                description = "Updated",
                distanceKM = 21.1,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<EventResponseDTO>();
            Assert.Equal("Renamed Event", updated!.EventName);
        }

        [Fact]
        public async Task UpdateEvent_AsDifferentOrganiser_Returns403()
        {
            var owner = await _factory.LoggedInClientAsync("Organiser");
            var other = await _factory.LoggedInClientAsync("Organiser");
            var ev = await owner.CreateEventAsync();

            var response = await other.PutAsJsonAsync($"/api/events/{ev.EventID}", ValidEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_AsParticipant_Returns403()
        {
            var owner = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await owner.CreateEventAsync();

            var response = await participant.PutAsJsonAsync($"/api/events/{ev.EventID}", ValidEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsOwner_RemovesTheEvent()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();

            var response = await organiser.DeleteAsync($"/api/events/{ev.EventID}");
            var afterwards = await _factory.NewClient().GetAsync($"/api/events/{ev.EventID}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsParticipant_Returns403()
        {
            var owner = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await owner.CreateEventAsync();

            var response = await participant.DeleteAsync($"/api/events/{ev.EventID}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_WithoutLogin_Returns401()
        {
            var owner = await _factory.LoggedInClientAsync("Organiser");
            var ev = await owner.CreateEventAsync();

            var response = await _factory.NewClient().DeleteAsync($"/api/events/{ev.EventID}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_ThatHasEnrolments_Returns409()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            await participant.EnrolAsync(category.CategoryID);

            var response = await organiser.DeleteAsync($"/api/events/{ev.EventID}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}
