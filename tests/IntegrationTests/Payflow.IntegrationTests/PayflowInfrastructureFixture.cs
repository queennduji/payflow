using Npgsql;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Payflow.IntegrationTests;

/// <summary>
/// Owns the three real backing containers every integration test class needs: one Postgres
/// *server* (each test class creates its own logical databases inside it — see
/// <see cref="CreateDatabaseAsync"/>), one RabbitMQ broker, and one Keycloak instance importing the
/// exact same realm the demo and docker-compose use, so there's only one place that realm is
/// defined. This fixture starts them; it does not itself build any service host — each test class
/// does that with <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>,
/// so different test classes can run the same services with different configuration (e.g. a
/// cranked-up fault rate) without paying to restart these containers.
/// </summary>
public sealed class PayflowInfrastructureFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    // A fresh, random credential per test run rather than a fixed literal: nothing here needs to
    // be remembered or typed by a person (unlike the docker-compose/.env credentials, which do),
    // so there's no reason for it to be a constant anyone could find by reading the source.
    private readonly string _rabbitMqPassword = Guid.NewGuid().ToString("N");

    // RabbitMQ's built-in "guest" account only accepts connections that arrive over the loopback
    // interface, as RabbitMQ itself sees it — which a container-to-host port mapping never counts
    // as, even from 127.0.0.1. A non-guest account sidesteps that restriction entirely.
    private readonly RabbitMqContainer _rabbitMq;

    private readonly KeycloakContainer _keycloak = new KeycloakBuilder("quay.io/keycloak/keycloak:26.0")
        .WithRealm(FindRealmExportPath())
        .Build();

    public PayflowInfrastructureFixture()
    {
        _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine")
            .WithUsername("payflow")
            .WithPassword(_rabbitMqPassword)
            .Build();
    }

    public string PostgresHost => _postgres.Hostname;
    public int PostgresPort => _postgres.GetMappedPublicPort(5432);
    public string RabbitMqHost => _rabbitMq.Hostname;
    public int RabbitMqPort => _rabbitMq.GetMappedPublicPort(5672);
    public string RabbitMqUsername => "payflow";
    public string RabbitMqPassword => _rabbitMqPassword;
    // GetBaseAddress() already ends in a trailing slash — naively appending another segment here
    // produces a double slash, which makes the issuer string JwtBearer validates against not
    // match the token's actual (single-slash) `iss` claim.
    public string KeycloakAuthority => $"{_keycloak.GetBaseAddress().TrimEnd('/')}/realms/payflow";
    public string KeycloakTokenEndpoint => $"{KeycloakAuthority}/protocol/openid-connect/token";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync(), _keycloak.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask(), _keycloak.DisposeAsync().AsTask());
    }

    /// <summary>
    /// Creates one logical database on the shared Postgres server and returns a connection string
    /// for it. One server rather than one container per service: what actually needs isolating is
    /// the schema (MassTransit's outbox tables are the same generic names in every service, so two
    /// services sharing one *database* would collide), not the server process.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = databaseName };
        return builder.ConnectionString;
    }

    private static string FindRealmExportPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Payflow.slnx")))
            directory = directory.Parent;

        return directory is null
            ? throw new InvalidOperationException("Could not locate the repository root from the test output directory.")
            : Path.Combine(directory.FullName, "deploy", "keycloak", "realm-export.json");
    }
}
