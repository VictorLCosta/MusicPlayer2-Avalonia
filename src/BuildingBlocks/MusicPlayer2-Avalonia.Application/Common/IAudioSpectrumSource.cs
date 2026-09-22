namespace MusicPlayer2_Avalonia.Application.Common;

public interface IAudioSpectrumSource
{
    // Normalized logarithmic frequency bands. Silence/unsupported sources return zeroes.
    void CopySpectrum(Span<float> destination);
}