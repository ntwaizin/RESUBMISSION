using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session (stores API cookie, user id and role). Custom cookie name so it
// never collides with the API's ".AspNetCore.Session" cookie on localhost.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Web.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
    throw new InvalidOperationException("ApiSettings:BaseUrl is missing from configuration.");

var allowUntrustedCert = builder.Configuration.GetValue<bool>("ApiSettings:AllowUntrustedCertificate");

builder.Services.AddHttpClient("RaceDayApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // UseCookies = false: the session cookie is attached manually per user,
    // so cookies must never be shared between users via a pooled handler.
    var handler = new HttpClientHandler { UseCookies = false };
    if (allowUntrustedCert)
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
    return handler;
});

builder.Services.AddScoped<RaceDayApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Inside Docker the container only listens on HTTP.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
