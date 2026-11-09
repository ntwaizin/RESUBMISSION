using System.Net;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    public class ApiResult
    {
        public bool Ok { get; set; }
        public HttpStatusCode Status { get; set; }
        public string? Error { get; set; }
        public string Body { get; set; } = "";
    }

    public class ApiResult<T> : ApiResult
    {
        public T? Data { get; set; }
    }

    /// <summary>All communication with the RaceDay API goes through here (no direct DB / Azure access in MVC).</summary>
    public class RaceDayApiService
    {
        private const string CookieKey = "RaceDayApiCookie";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RaceDayApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        // ---------- Authentication ----------
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("RaceDayApi");
                using var content = new StringContent(
                    JsonSerializer.Serialize(new { email, password }, JsonOptions), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(ApiRoutes.Login, content);

                if (!response.IsSuccessStatusCode) return false;

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var role = GetProp(doc.RootElement, "role")?.GetString();
                var userId = GetProp(doc.RootElement, "userID")?.GetInt32();

                if (string.IsNullOrEmpty(role) || userId == null) return false;

                // Keep every cookie the API issued (its session cookie) and replay it on later calls.
                if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return false;
                var cookieHeader = string.Join("; ", cookies.Select(c => c.Split(';')[0]));

                Session.SetString(CookieKey, cookieHeader);
                Session.SetString("UserRole", role);
                Session.SetInt32("UserID", userId.Value);
                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or TaskCanceledException)
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try { await CoreAsync(HttpMethod.Post, ApiRoutes.Logout, null); }
            finally { Session.Clear(); }
        }

        private static JsonElement? GetProp(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;
            foreach (var p in root.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    return p.Value;
            return null;
        }

        // ---------- Typed helpers ----------
        public async Task<ApiResult<T>> GetAsync<T>(string endpoint)
        {
            var r = await CoreAsync(HttpMethod.Get, endpoint, null);
            return ToTyped<T>(r);
        }

        public Task<ApiResult> PostAsync(string endpoint, object body) => CoreAsync(HttpMethod.Post, endpoint, Json(body));
        public Task<ApiResult> PutAsync(string endpoint, object body) => CoreAsync(HttpMethod.Put, endpoint, Json(body));
        public Task<ApiResult> DeleteAsync(string endpoint) => CoreAsync(HttpMethod.Delete, endpoint, null);

        public async Task<ApiResult<T>> PostAsync<T>(string endpoint, object body)
            => ToTyped<T>(await CoreAsync(HttpMethod.Post, endpoint, Json(body)));

        /// <summary>Sends a file to the API (the API stores it in Azure Blob Storage).</summary>
        public async Task<ApiResult> UploadFileAsync(string endpoint, IFormFile file, string fieldName = "file")
        {
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(fileContent, fieldName, file.FileName);
            return await CoreAsync(HttpMethod.Post, endpoint, form);
        }

        private static StringContent Json(object body)
            => new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        private async Task<ApiResult> CoreAsync(HttpMethod method, string endpoint, HttpContent? content)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("RaceDayApi");
                using var request = new HttpRequestMessage(method, endpoint) { Content = content };

                var cookie = Session.GetString(CookieKey);
                if (!string.IsNullOrEmpty(cookie)) request.Headers.Add("Cookie", cookie);

                using var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    Session.Clear();

                return new ApiResult
                {
                    Ok = response.IsSuccessStatusCode,
                    Status = response.StatusCode,
                    Body = body,
                    Error = response.IsSuccessStatusCode ? null : FriendlyError(response.StatusCode, body)
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                return new ApiResult
                {
                    Ok = false,
                    Status = HttpStatusCode.ServiceUnavailable,
                    Error = "The RaceDay service is unavailable right now. Please try again shortly."
                };
            }
        }

        private static ApiResult<T> ToTyped<T>(ApiResult r)
        {
            var typed = new ApiResult<T> { Ok = r.Ok, Status = r.Status, Error = r.Error, Body = r.Body };
            if (r.Ok && !string.IsNullOrWhiteSpace(r.Body))
            {
                try { typed.Data = JsonSerializer.Deserialize<T>(r.Body, JsonOptions); }
                catch (JsonException)
                {
                    typed.Ok = false;
                    typed.Error = "The server returned data in an unexpected format.";
                }
            }
            return typed;
        }

        // Never show raw API / exception text to the user.
        private static string FriendlyError(HttpStatusCode status, string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var key in new[] { "message", "error", "detail" })
                {
                    var p = GetProp(doc.RootElement, key);
                    if (p?.ValueKind == JsonValueKind.String && p.Value.GetString()!.Length < 200)
                        return p.Value.GetString()!;
                }
            }
            catch (JsonException) { }

            return status switch
            {
                HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
                HttpStatusCode.Forbidden => "You do not have permission to do that.",
                HttpStatusCode.NotFound => "The requested item could not be found.",
                HttpStatusCode.Conflict => "That action conflicts with existing data (for example, already enrolled).",
                HttpStatusCode.BadRequest => "Some of the details were invalid. Please check and try again.",
                _ => "Something went wrong. Please try again."
            };
        }
    }
}
