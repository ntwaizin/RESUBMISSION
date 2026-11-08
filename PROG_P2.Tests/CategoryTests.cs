using System.Net;
using System.Net.Http.Json;
using PROG_P2.DTOs;

namespace PROG_P2.Tests
{
    /// <summary>Category management for events.</summary>
    public class CategoryTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public CategoryTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateCategory_AsOwningOrganiser_Returns201()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();

            var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventID}/categories", new
            {
                categoryName = "21km",
                distanceKM = 21.1,
                entryFee = 250.0
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<CategoryResponseDTO>();
            Assert.Equal(ev.EventID, created!.EventID);
        }

        [Fact]
        public async Task CreateCategory_ForAnotherOrganisersEvent_Returns403()
        {
            var owner = await _factory.LoggedInClientAsync("Organiser");
            var other = await _factory.LoggedInClientAsync("Organiser");
            var ev = await owner.CreateEventAsync();

            var response = await other.PostAsJsonAsync($"/api/events/{ev.EventID}/categories", new
            {
                categoryName = "Sneaky",
                distanceKM = 5.0,
                entryFee = 10.0
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_WithNegativeFee_Returns400()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();

            var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventID}/categories", new
            {
                categoryName = "Bad",
                distanceKM = 5.0,
                entryFee = -50.0
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCategories_ForEvent_IsPublic()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();
            await organiser.CreateCategoryAsync(ev.EventID);

            var list = await _factory.NewClient().GetFromJsonAsync<List<CategoryResponseDTO>>($"/api/events/{ev.EventID}/categories");

            Assert.Single(list!);
        }

        [Fact]
        public async Task UpdateCategory_AsOwningOrganiser_Returns200()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);

            var response = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryID}", new
            {
                categoryName = "Under 20",
                distanceKM = 5.0,
                entryFee = 80.0
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<CategoryResponseDTO>();
            Assert.Equal("Under 20", updated!.CategoryName);
        }

        [Fact]
        public async Task DeleteCategory_AsOwningOrganiser_RemovesIt()
        {
            var organiser = await _factory.LoggedInClientAsync("Organiser");
            var ev = await organiser.CreateEventAsync();
            var category = await organiser.CreateCategoryAsync(ev.EventID);

            var response = await organiser.DeleteAsync($"/api/categories/{category.CategoryID}");
            var afterwards = await _factory.NewClient().GetAsync($"/api/categories/{category.CategoryID}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
        }
    }
}
