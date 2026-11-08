using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROG_P2.Data;

namespace PROG_P2.Tests
{
    /// <summary>
    /// Starts the real API in memory for the tests. SQL Server is swapped for an
    /// in-memory database, so the tests run anywhere (including GitHub Actions).
    /// Everything else (controllers, session, role checks, password hashing) is the real code.
    /// </summary>
    public class RaceDayFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = "RaceDayTests-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:RaceDayConnection",
                "Server=(localdb)\\mssqllocaldb;Database=NotUsedInTests;Trusted_Connection=True;");

            builder.ConfigureTestServices(services =>
            {
                var existing = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>))
                    .ToList();
                foreach (var descriptor in existing)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<RaceDayContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
