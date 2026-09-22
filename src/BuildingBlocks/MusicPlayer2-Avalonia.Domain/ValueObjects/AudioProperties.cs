namespace MusicPlayer2_Avalonia.Domain.ValueObjects;

public sealed record AudioProperties(
    TimeSpan Duration,
    int BitrateKbps,
    int SampleRateHz,
    int Channels,
    int BitDepth);