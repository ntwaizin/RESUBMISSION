using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROG_P2.Data;
using PROG_P2.DTOs;

namespace PROG_P2.Tests
{
    /// <summary>Registration, login, logout, sessions and profile.</summary>
    public class AuthTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public AuthTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("Organiser")]
        [InlineData("Participant")]
        public async Task Register_WithValidDetails_Returns201(string role)
        {
            var client = _factory.NewClient();

            var response = await client.RegisterAsync(role, TestSupport.UniqueEmail("reg"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
            Assert.Equal(role, body!.Role);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_Returns409()
        {
            var client = _factory.NewClient();
            var email = TestSupport.UniqueEmail("dup");
            (await client.RegisterAsync("Participant", email)).EnsureSuccessStatusCode();

            var second = await client.RegisterAsync("Organiser", email);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Register_WithInvalidRole_Returns400()
        {
            var client = _factory.NewClient();

            var response = await client.RegisterAsync("Admin", TestSupport.UniqueEmail("badrole"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithShortPassword_Returns400()
        {
            var client = _factory.NewClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "A",
                lastName = "B",
                email = TestSupport.UniqueEmail("short"),
                password = "123",
                role = "Participant"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_StoresHashedPassword_NotThePlainPassword()
        {
            var client = _factory.NewClient();
            var email = TestSupport.UniqueEmail("hash");
            (await client.RegisterAsync("Participant", email)).EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            var account = await db.Accounts.SingleAsync(a => a.Email == email);

            Assert.NotEqual(TestSupport.Password, account.PasswordHash);
            Assert.False(string.IsNullOrWhiteSpace(account.PasswordHash));
        }

        [Fact]
        public async Task Login_WithCorrectCredentials_Returns200AndRole()
        {
            var client = _factory.NewClient();
            var email = TestSupport.UniqueEmail("login");
            (await client.RegisterAsync("Organiser", email)).EnsureSuccessStatusCode();

            var response = await client.LoginAsync(email);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
            Assert.Equal("Organiser", body!.Role);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var client = _factory.NewClient();
            var email = TestSupport.UniqueEmail("wrongpw");
            (await client.RegisterAsync("Participant", email)).EnsureSuccessStatusCode();

            var response = await client.LoginAsync(email, "NotMyPassword1!");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_Returns401()
        {
            var client = _factory.NewClient();

            var response = await client.LoginAsync(TestSupport.UniqueEmail("ghost"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Profile_WithoutLogin_Returns401()
        {
            var client = _factory.NewClient();

            var response = await client.GetAsync("/api/profile");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Profile_AfterLogin_ReturnsOwnProfile()
        {
            var client = await _factory.LoggedInClientAsync("Participant");

            var response = await client.GetAsync("/api/profile");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var profile = await response.Content.ReadFromJsonAsync<ProfileResponseDTO>();
            Assert.Equal("Participant", profile!.Role);
        }

        [Fact]
        public async Task Logout_ClearsTheSession()
        {
            var client = await _factory.LoggedInClientAsync("Participant");
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile")).StatusCode);

            var logout = await client.PostAsync("/api/auth/logout", null);
            var afterLogout = await client.GetAsync("/api/profile");

            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_ChangesTheStoredDetails()
        {
            var client = await _factory.LoggedInClientAsync("Participant");

            var update = await client.PutAsJsonAsync("/api/profile", new
            {
                firstName = "Updated",
                lastName = "Name",
                email = TestSupport.UniqueEmail("updated"),
                phone = "0719998888",
                dateOfBirth = new DateTime(1990, 5, 5)
            });
            var profile = await client.GetFromJsonAsync<ProfileResponseDTO>("/api/profile");

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            Assert.Equal("Updated", profile!.FirstName);
            Assert.Equal("0719998888", profile.Phone);
        }

        [Fact]
        public async Task UpdateProfile_WithoutLogin_Returns401()
        {
            var client = _factory.NewClient();

            var response = await client.PutAsJsonAsync("/api/profile", new
            {
                firstName = "X",
                lastName = "Y",
                email = TestSupport.UniqueEmail("nologin")
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
