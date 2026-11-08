using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PROG_P2.DTOs;

namespace PROG_P2.Tests
{
    /// <summary>Small helpers so each test reads like the scenario it checks.</summary>
    public static class TestSupport
    {
        public const string Password = "Password123!";

        public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.co.za";

        /// <summary>A new client with its own cookie jar (= its own session).</summary>
        public static HttpClient NewClient(this RaceDayFactory factory) =>
            factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost")
            });

        public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string role, string email) =>
            client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Test",
                lastName = role,
                email,
                phone = "0821234567",
                dateOfBirth = new DateTime(1995, 1, 1),
                password = Password,
                role
            });

        public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = Password) =>
            client.PostAsJsonAsync("/api/auth/login", new { email, password });

        /// <summary>Registers a new user of the given role and logs them in.</summary>
        public static async Task<HttpClient> LoggedInClientAsync(this RaceDayFactory factory, string role)
        {
            var client = factory.NewClient();
            var email = UniqueEmail(role.ToLowerInvariant());

            (await client.RegisterAsync(role, email)).EnsureSuccessStatusCode();
            (await client.LoginAsync(email)).EnsureSuccessStatusCode();
            return client;
        }

        public static async Task<int> UserIdAsync(this HttpClient client)
        {
            var profile = await client.GetFromJsonAsync<ProfileResponseDTO>("/api/profile");
            return profile!.UserID;
        }

        public static async Task<EventResponseDTO> CreateEventAsync(this HttpClient organiser, string eventType = "Run")
        {
            var response = await organiser.PostAsJsonAsync("/api/events", new
            {
                eventName = "Test Event " + Guid.NewGuid().ToString("N")[..6],
                eventDate = DateTime.UtcNow.AddDays(30),
                location = "Johannesburg",
                description = "Integration test event",
                distanceKM = 10.0,
                eventType
            });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EventResponseDTO>())!;
        }

        public static async Task<CategoryResponseDTO> CreateCategoryAsync(this HttpClient organiser, int eventId)
        {
            var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
            {
                categoryName = "Main Race",
                distanceKM = 10.0,
                entryFee = 150.0
            });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<CategoryResponseDTO>())!;
        }

        public static async Task<EnrolmentResponseDTO> EnrolAsync(this HttpClient participant, int categoryId)
        {
            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EnrolmentResponseDTO>())!;
        }
    }
}
