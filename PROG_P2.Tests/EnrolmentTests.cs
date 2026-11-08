using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROG_P2.Data;
using PROG_P2.DTOs;

namespace PROG_P2.Tests
{
    /// <summary>Event enrolments: participants enter events and the link is recorded correctly.</summary>
    public class EnrolmentTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public EnrolmentTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Enrol_AsParticipant_Returns201AndRecordsParticipantEventAndCategory()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            var participantId = await participant.UserIdAsync();

            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<EnrolmentResponseDTO>();
            Assert.Equal(participantId, created!.ParticipantID);
            Assert.Equal(ev.EventID, created.EventID);
            Assert.Equal(category.CategoryID, created.CategoryID);
            Assert.Equal("Pending", created.PaymentStatus);

            // Check the row really is in the database
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            var saved = await db.Enrolments.SingleAsync(e => e.EnrolmentID == created.EnrolmentID);
            Assert.Equal(participantId, saved.ParticipantID);
            Assert.Equal(category.CategoryID, saved.CategoryID);
        }

        [Fact]
        public async Task Enrol_WithoutLogin_Returns401()
        {
            var response = await _factory.NewClient().PostAsJsonAsync("/api/enrolments", new { categoryId = 1 });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_InCategoryThatDoesNotExist_Returns404()
        {
            var participant = await _factory.LoggedInClientAsync("Participant");

            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = 999999 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_WithInvalidCategoryId_Returns400()
        {
            var participant = await _factory.LoggedInClientAsync("Participant");

            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = 0 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_TwiceInTheSameEvent_Returns409()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            await participant.EnrolAsync(category.CategoryID);

            var second = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task MyEnrolments_ListsOnlyTheLoggedInParticipantsEnrolments()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var alice = await _factory.LoggedInClientAsync("Participant");
            var bob = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            var aliceEnrolment = await alice.EnrolAsync(category.CategoryID);
            await bob.EnrolAsync(category.CategoryID);

            var mine = await alice.GetFromJsonAsync<List<EnrolmentResponseDTO>>("/api/enrolments");

            Assert.Single(mine!);
            Assert.Equal(aliceEnrolment.EnrolmentID, mine![0].EnrolmentID);
        }

        [Fact]
        public async Task Organiser_CanViewEnrolmentsForTheirEvent()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var alice = await _factory.LoggedInClientAsync("Participant");
            var bob = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            await alice.EnrolAsync(category.CategoryID);
            await bob.EnrolAsync(category.CategoryID);

            var response = await organiser.GetAsync($"/api/events/{ev.EventID}/enrolments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponseDTO>>();
            Assert.Equal(2, list!.Count);
        }

        [Fact]
        public async Task Organiser_CannotViewEnrolmentsForAnotherOrganisersEvent_Returns403()
        {
            var owner = await _factory.LoggedInClientAsync("Organiser");
            var other = await _factory.LoggedInClientAsync("Organiser");
            var ev = await owner.CreateEventAsync();

            var response = await other.GetAsync($"/api/events/{ev.EventID}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CanCancelTheirOwnEnrolment()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var participant = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            var enrolment = await participant.EnrolAsync(category.CategoryID);

            var response = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentID}");
            var remaining = await participant.GetFromJsonAsync<List<EnrolmentResponseDTO>>("/api/enrolments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(remaining!);
        }

        [Fact]
        public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns404()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var alice = await _factory.LoggedInClientAsync("Participant");
            var bob = await _factory.LoggedInClientAsync("Participant");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);
            var aliceEnrolment = await alice.EnrolAsync(category.CategoryID);

            var response = await bob.DeleteAsync($"/api/enrolments/{aliceEnrolment.EnrolmentID}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
