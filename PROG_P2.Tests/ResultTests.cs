using System.Net;
using System.Net.Http.Json;
using PROG_P2.DTOs;

namespace PROG_P2.Tests
{
    /// <summary>Results: organisers capture them, participants view their own.</summary>
    public class ResultTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public ResultTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        private async Task<(HttpClient Organiser, HttpClient Participant, EnrolmentResponseDTO Enrolment)> SetUpAsync()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            var enrolment = await participant.EnrolAsync(category.CategoryID);
            return (organiser, participant, enrolment);
        }

        [Fact]
        public async Task RecordResult_AsOwningOrganiser_Returns201()
        {
            var (organiser, _, enrolment) = await SetUpAsync();

            var response = await organiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = enrolment.EnrolmentID,
                finishTime = "00:28:35",
                position = 15,
                resultStatus = "Finished"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ResultResponseDTO>();
            Assert.Equal(15, result!.Position);
            Assert.Equal(new TimeSpan(0, 28, 35), result.FinishTime);
        }

        [Fact]
        public async Task RecordResult_ForAnotherOrganisersEvent_Returns403()
        {
            var (_, _, enrolment) = await SetUpAsync();
            var otherOrganiser = await _factory.LoggedInClientAsync("Organiser");

            var response = await otherOrganiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = enrolment.EnrolmentID,
                finishTime = "00:30:00",
                position = 1,
                resultStatus = "Finished"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RecordResult_Twice_Returns409()
        {
            var (organiser, _, enrolment) = await SetUpAsync();
            var body = new { enrolmentId = enrolment.EnrolmentID, finishTime = "00:40:00", position = 3, resultStatus = "Finished" };
            (await organiser.PostAsJsonAsync("/api/results", body)).EnsureSuccessStatusCode();

            var second = await organiser.PostAsJsonAsync("/api/results", body);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task RecordResult_WithInvalidPosition_Returns400()
        {
            var (organiser, _, enrolment) = await SetUpAsync();

            var response = await organiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = enrolment.EnrolmentID,
                finishTime = "00:40:00",
                position = 0,
                resultStatus = "Finished"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RecordResult_WithInvalidStatus_Returns400()
        {
            var (organiser, _, enrolment) = await SetUpAsync();

            var response = await organiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = enrolment.EnrolmentID,
                finishTime = "00:40:00",
                position = 2,
                resultStatus = "Winner"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RecordResult_ForUnknownEnrolment_Returns404()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");

            var response = await organiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = 999999,
                finishTime = "00:40:00",
                position = 2,
                resultStatus = "Finished"
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Participant_SeesOnlyTheirOwnResults()
        {
            var (organiser, participant, enrolment) = await SetUpAsync();
            var someoneElse = await _factory.LoggedInClientAsync("Participant");
            (await organiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = enrolment.EnrolmentID,
                finishTime = "00:45:10",
                position = 8,
                resultStatus = "Finished"
            })).EnsureSuccessStatusCode();

            var mine = await participant.GetFromJsonAsync<List<ResultResponseDTO>>("/api/results/mine");
            var theirs = await someoneElse.GetFromJsonAsync<List<ResultResponseDTO>>("/api/results/mine");

            Assert.Single(mine!);
            Assert.Equal(8, mine![0].Position);
            Assert.Empty(theirs!);
        }

        [Fact]
        public async Task UpdateResult_AsOwningOrganiser_ChangesTheStoredResult()
        {
            var (organiser, _, enrolment) = await SetUpAsync();
            var created = await (await organiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = enrolment.EnrolmentID,
                finishTime = "00:50:00",
                position = 20,
                resultStatus = "Finished"
            })).Content.ReadFromJsonAsync<ResultResponseDTO>();

            var response = await organiser.PutAsJsonAsync($"/api/results/{created!.ResultID}", new
            {
                finishTime = "00:48:30",
                position = 18,
                resultStatus = "Finished"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<ResultResponseDTO>();
            Assert.Equal(18, updated!.Position);
        }

        [Fact]
        public async Task UpdateResult_AsParticipant_Returns403()
        {
            var (organiser, participant, enrolment) = await SetUpAsync();
            var created = await (await organiser.PostAsJsonAsync("/api/results", new
            {
                enrolmentId = enrolment.EnrolmentID,
                finishTime = "00:50:00",
                position = 20,
                resultStatus = "Finished"
            })).Content.ReadFromJsonAsync<ResultResponseDTO>();

            var response = await participant.PutAsJsonAsync($"/api/results/{created!.ResultID}", new
            {
                finishTime = "00:10:00",
                position = 1,
                resultStatus = "Finished"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetResults_WithoutLogin_Returns200()
        {
            var response = await _factory.NewClient().GetAsync("/api/results");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
