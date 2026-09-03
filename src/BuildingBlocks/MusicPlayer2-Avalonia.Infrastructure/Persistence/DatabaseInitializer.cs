namespace MusicPlayer2_Avalonia.Infrastructure.Persistence;

public sealed class DatabaseInitializer(MusicPlayerDbContext context)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        // EnsureCreated does not evolve an existing SQLite schema.
        await AddColumnIfMissingAsync("SourceFileSizeBytes", "INTEGER", cancellationToken).ConfigureAwait(false);
        await AddColumnIfMissingAsync("SourceLastWriteTimeUtc", "TEXT", cancellationToken).ConfigureAwait(false);
    }

    private async Task AddColumnIfMissingAsync(
        string columnName,
        string columnType,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        bool closeConnection = connection.State != System.Data.ConnectionState.Open;

        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Tracks') WHERE name = $columnName";
            var parameter = checkCommand.CreateParameter();
            parameter.ParameterName = "$columnName";
            parameter.Value = columnName;
            checkCommand.Parameters.Add(parameter);

            var columnExists = Convert.ToInt64(
                await checkCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;

            if (columnExists)
            {
                return;
            }

            await using var addColumnCommand = connection.CreateCommand();
            addColumnCommand.CommandText = $"ALTER TABLE Tracks ADD COLUMN {columnName} {columnType} NULL";
            await addColumnCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
