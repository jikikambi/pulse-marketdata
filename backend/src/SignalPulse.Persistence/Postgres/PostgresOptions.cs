namespace SignalPulse.Persistence.Postgres;

public sealed class PostgresOptions
{
    public string ConnectionString { get; set; } = default!;
    public int CommandTimeoutSeconds { get; set; } = 30;
}