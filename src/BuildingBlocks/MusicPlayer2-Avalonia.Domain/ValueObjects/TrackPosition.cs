namespace MusicPlayer2_Avalonia.Domain.ValueObjects;

public readonly record struct TrackPosition(int DiscNumber, int TrackNumber)
{
    public static TrackPosition Create(int discNumber, int trackNumber)
    {
        if (discNumber < 1 || trackNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(trackNumber));

        return new TrackPosition(discNumber, trackNumber);
    }
}
