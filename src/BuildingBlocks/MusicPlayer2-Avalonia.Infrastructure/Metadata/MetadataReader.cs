using MusicPlayer2_Avalonia.Application.Common;
using TagLibFile = TagLib.File;

namespace MusicPlayer2_Avalonia.Infrastructure.Metadata;

public class MetadataReader : IMetaDataReader
{
    public async Task<AudioMetadata> Read(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var audioFile = TagLibFile.Create(filePath);
                var properties = audioFile.Properties;
                var tag = audioFile.Tag;

                return new AudioMetadata(
                    Title: string.IsNullOrWhiteSpace(tag.Title)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : tag.Title,
                    Artist: tag.FirstPerformer,
                    Album: tag.Album,
                    Genre: tag.FirstGenre,
                    ReleaseYear: tag.Year == 0 ? null : (int)tag.Year,
                    Duration: properties.Duration,
                    BitrateKbps: properties.AudioBitrate,
                    SampleRateHz: properties.AudioSampleRate,
                    Channels: properties.AudioChannels
                );
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new MetadataReadException(filePath, exception);
        }
    }
}
