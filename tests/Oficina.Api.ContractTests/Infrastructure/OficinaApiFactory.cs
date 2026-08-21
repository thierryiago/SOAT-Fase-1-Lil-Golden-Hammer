using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Oficina.Infrastructure.Persistence;
using Oficina.Application.Notifications;

namespace Oficina.Api.ContractTests.Infrastructure;

public sealed class OficinaApiFactory : WebApplicationFactory<Program>
{
    public const string JwtIssuer = "Oficina.Api.Tests";
    public const string JwtAudience = "Oficina.Administration.Tests";
    public const string JwtSigningKey = "contract-tests-only-signing-key-at-least-32-bytes";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["Jwt:ExpirationMinutes"] = "15"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"oficina-contracts-{Guid.NewGuid()}"));
            services.RemoveAll<INotificationEmailSender>();
            services.AddScoped<INotificationEmailSender, FakeNotificationEmailSender>();
        });
    }

    private sealed class FakeNotificationEmailSender : INotificationEmailSender
    {
        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
