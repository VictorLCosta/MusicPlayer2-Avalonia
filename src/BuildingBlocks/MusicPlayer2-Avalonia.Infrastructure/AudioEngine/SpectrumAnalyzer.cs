using System.Numerics;

namespace MusicPlayer2_Avalonia.Infrastructure.AudioEngine;

internal sealed class SpectrumAnalyzer
{
    public const int Size = 2048;
    public const int BandCount = 64;
    public const int SampleRate = 44100;
    private readonly Complex[] _fft = new Complex[Size];

    public float[] Analyze(ReadOnlySpan<float> samples)
    {
        for (int i = 0; i < Size; i++)
            _fft[i] = samples[i] * (0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (Size - 1)));
        for (int i = 1, j = 0; i < Size; i++)
        {
            int bit = Size >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) (_fft[i], _fft[j]) = (_fft[j], _fft[i]);
        }
        for (int length = 2; length <= Size; length <<= 1)
        {
            var step = Complex.FromPolarCoordinates(1, -2 * Math.PI / length);
            for (int start = 0; start < Size; start += length)
            {
                var phase = Complex.One;
                for (int j = 0; j < length / 2; j++)
                {
                    var even = _fft[start + j];
                    var odd = _fft[start + j + length / 2] * phase;
                    _fft[start + j] = even + odd;
                    _fft[start + j + length / 2] = even - odd;
                    phase *= step;
                }
            }
        }
        var bands = new float[BandCount];
        for (int band = 0; band < BandCount; band++)
        {
            int low = Math.Max(1, (int)(40 * Math.Pow(400, band / (double)BandCount) * Size / SampleRate));
            int high = Math.Min(Size / 2, Math.Max(low + 1,
                (int)(40 * Math.Pow(400, (band + 1d) / BandCount) * Size / SampleRate)));
            double magnitude = 0;
            for (int bin = low; bin < high; bin++) magnitude = Math.Max(magnitude, _fft[bin].Magnitude * 4 / Size);
            bands[band] = (float)Math.Clamp((20 * Math.Log10(Math.Max(magnitude, 1e-8)) + 70) / 70, 0, 1);
        }
        return bands;
    }
}