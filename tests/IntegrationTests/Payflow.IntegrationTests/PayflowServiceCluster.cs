extern alias AuthorizationApi;
extern alias FraudApi;
extern alias LedgerApi;
extern alias NotificationsApi;
extern alias PaymentsApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using AuthorizationProgram = AuthorizationApi::Program;
using FraudProgram = FraudApi::Program;
using LedgerProgram = LedgerApi::Program;
using NotificationsProgram = NotificationsApi::Program;
using PaymentsProgram = PaymentsApi::Program;

namespace Payflow.IntegrationTests;

/// <summary>
/// Boots the five saga-participant services in-process against the shared fixture's real
/// Postgres/RabbitMQ/Keycloak, each on its own logical database. This is the "cluster" a test class
/// talks to – built once per test class (not per fixture), so one class can run Authorization at
/// its default 0% fault rate while another cranks it up, without restarting any container.
/// Gateway isn't hosted here: it's a routing layer with nothing left to prove once the services it
/// proxies to are already being exercised directly. Vault isn't a saga participant.
/// </summary>
public sealed class PayflowServiceCluster : IAsyncDisposable
{
    public const string Audience = "payflow-api";

    private readonly WebApplicationFactory<PaymentsProgram> _payments;
    private readonly WebApplicationFactory<AuthorizationProgram> _authorization;
    private readonly WebApplicationFactory<LedgerProgram> _ledger;
    private readonly WebApplicationFactory<FraudProgram> _fraud;
    private readonly WebApplicationFactory<NotificationsProgram> _notifications;

    public HttpClient PaymentsClient { get; }
    public HttpClient LedgerClient { get; }
    public HttpClient NotificationsClient { get; }

    private PayflowServiceCluster(
        WebApplicationFactory<PaymentsProgram> payments,
        WebApplicationFactory<AuthorizationProgram> authorization,
        WebApplicationFactory<LedgerProgram> ledger,
        WebApplicationFactory<FraudProgram> fraud,
        WebApplicationFactory<NotificationsProgram> notifications,
        HttpClient paymentsClient,
        HttpClient ledgerClient,
        HttpClient notificationsClient)
    {
        _payments = payments;
        _authorization = authorization;
        _ledger = ledger;
        _fraud = fraud;
        _notifications = notifications;

        PaymentsClient = paymentsClient;
        LedgerClient = ledgerClient;
        NotificationsClient = notificationsClient;
    }

    public static async Task<PayflowServiceCluster> StartAsync(PayflowInfrastructureFixture fixture, double authorizationFaultRate = 0.0)
    {
        var paymentsDb = await fixture.CreateDatabaseAsync($"payments_test_{Guid.NewGuid():N}");
        var authorizationDb = await fixture.CreateDatabaseAsync($"authorization_test_{Guid.NewGuid():N}");
        var ledgerDb = await fixture.CreateDatabaseAsync($"ledger_test_{Guid.NewGuid():N}");
        var fraudDb = await fixture.CreateDatabaseAsync($"fraud_test_{Guid.NewGuid():N}");
        var notificationsDb = await fixture.CreateDatabaseAsync($"notifications_test_{Guid.NewGuid():N}");

        // ASP.NET Core minimal APIs read configuration (here: Authentication:Authority, inside
        // AddPayflowAuthentication) synchronously while Program.cs's top-level statements run, well
        // before WebApplicationFactory's own ConfigureAppConfiguration customization is applied to
        // the builder – that hook fires too late to affect a value already read into a local
        // variable. Environment variables are loaded by WebApplication.CreateBuilder itself, so
        // they're visible from the very first line of Program.cs, which a plain config-override
        // callback can't guarantee for every value every service reads eagerly at startup.
        var payments = BuildFactory<PaymentsProgram>(new()
        {
            ["ConnectionStrings__PaymentsDb"] = paymentsDb,
        }, fixture, out var paymentsClient);

        var authorization = BuildFactory<AuthorizationProgram>(new()
        {
            ["ConnectionStrings__AuthorizationDb"] = authorizationDb,
            ["Chaos__CardNetworkFaultRate"] = authorizationFaultRate.ToString(),
        }, fixture, out var authorizationClient);

        var ledger = BuildFactory<LedgerProgram>(new()
        {
            ["ConnectionStrings__LedgerDb"] = ledgerDb,
        }, fixture, out var ledgerClient);

        var fraud = BuildFactory<FraudProgram>(new()
        {
            ["ConnectionStrings__FraudDb"] = fraudDb,
        }, fixture, out var fraudClient);

        var notifications = BuildFactory<NotificationsProgram>(new()
        {
            ["ConnectionStrings__NotificationsDb"] = notificationsDb,
        }, fixture, out var notificationsClient);

        // Authorization and Fraud have no HTTP surface this test suite calls directly – they only
        // need to actually be running (migrated, bus connected) for the saga to reach them. Warming
        // them up here, rather than lazily, keeps every service's startup cost inside StartAsync.
        authorizationClient.Dispose();
        fraudClient.Dispose();

        return new PayflowServiceCluster(payments, authorization, ledger, fraud, notifications, paymentsClient, ledgerClient, notificationsClient);
    }

    private static WebApplicationFactory<TProgram> BuildFactory<TProgram>(
        Dictionary<string, string> serviceSpecificEnv, PayflowInfrastructureFixture fixture, out HttpClient client)
        where TProgram : class
    {
        var env = new Dictionary<string, string>(serviceSpecificEnv)
        {
            ["RabbitMq__Host"] = fixture.RabbitMqHost,
            ["RabbitMq__Port"] = fixture.RabbitMqPort.ToString(),
            ["RabbitMq__Username"] = fixture.RabbitMqUsername,
            ["RabbitMq__Password"] = fixture.RabbitMqPassword,
            ["Authentication__Authority"] = fixture.KeycloakAuthority,
            ["Authentication__Audience"] = Audience,
        };

        var previous = new Dictionary<string, string?>();
        foreach (var (key, value) in env)
        {
            previous[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        try
        {
            var factory = new WebApplicationFactory<TProgram>().WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
            client = factory.CreateClient(); // forces the host to build now, while the env vars above are in effect
            return factory;
        }
        finally
        {
            foreach (var (key, value) in previous)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        PaymentsClient.Dispose();
        LedgerClient.Dispose();
        NotificationsClient.Dispose();

        await _payments.DisposeAsync();
        await _authorization.DisposeAsync();
        await _ledger.DisposeAsync();
        await _fraud.DisposeAsync();
        await _notifications.DisposeAsync();
    }
}
