using System.Net;
using System.Net.Http.Json;

namespace PROG_P2.Tests
{
    /// <summary>
    /// Role-based access: Organiser-only endpoints reject Participants (403),
    /// Participant-only endpoints reject Organisers (403) and anonymous users get 401.
    /// </summary>
    public class RoleEnforcementTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public RoleEnforcementTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        private static object CategoryBody => new { categoryName = "Fun Run", distanceKM = 5.0, entryFee = 100.0 };

        [Fact]
        public async Task Participant_CannotCreateCategory_Returns403()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();

            var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventID}/categories", CategoryBody);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotUpdateOrDeleteCategory_Returns403()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);

            var update = await participant.PutAsJsonAsync($"/api/categories/{category.CategoryID}", CategoryBody);
            var delete = await participant.DeleteAsync($"/api/categories/{category.CategoryID}");

            Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotEnrol_Returns403()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);

            var response = await organiser.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotViewParticipantEnrolmentList_Returns403()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");

            var response = await organiser.GetAsync("/api/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotViewEventEnrolments_Returns403()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();

            var response = await participant.GetAsync($"/api/events/{ev.EventID}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotRecordResult_Returns403()
        {
            var participant = await _factory.LoggedInClientAsync("Participant");

            var response = await participant.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = 1,
                finishTime = "00:30:00",
                position = 1,
                resultStatus = "Finished"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotViewParticipantResults_Returns403()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");

            var response = await organiser.GetAsync("/api/results/mine");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("POST", "/api/enrolments")]
        [InlineData("GET", "/api/enrolments")]
        [InlineData("POST", "/api/results")]
        [InlineData("GET", "/api/results/mine")]
        [InlineData("POST", "/api/events/1/categories")]
        [InlineData("GET", "/api/events/1/enrolments")]
        [InlineData("GET", "/api/profile")]
        public async Task ProtectedEndpoints_WithoutLogin_Return401(string method, string url)
        {
            var client = _factory.NewClient();
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method == "POST")
            {
                request.Content = JsonContent.Create(new { });
            }

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
