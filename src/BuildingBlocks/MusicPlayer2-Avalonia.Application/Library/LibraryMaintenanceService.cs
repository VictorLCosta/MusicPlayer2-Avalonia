using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Application.Settings.Models;

namespace MusicPlayer2_Avalonia.Application.Library;

public sealed class LibraryMaintenanceService(IServiceScopeFactory scopeFactory) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<string?> UpdateAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var problems = new List<string>();
            foreach (var folder in settings.LibraryFolders.Distinct(
                         OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal))
            {
                // Use a separate context from playback and discard it after each folder.
                await using var scope = scopeFactory.CreateAsyncScope();
                var library = scope.ServiceProvider.GetRequiredService<LibraryService>();
                try
                {
                    var result = await library.ScanDirectoryAsync(folder).ConfigureAwait(false);
                    if (result.Failed > 0) problems.Add($"{folder}: {result.Failed} arquivo(s) não puderam ser lidos.");
                    if (settings.RemoveMissingFilesFromLibrary)
                        await library.RemoveMissingTracksAsync(folder).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    problems.Add($"Não foi possível atualizar a pasta: {folder}");
                }
            }
            return problems.Count == 0 ? null : string.Join(Environment.NewLine, problems);
        }
        finally { _gate.Release(); }
    }

    public void Dispose() => _gate.Dispose();
}