namespace MusicPlayer2_Avalonia.Domain.ValueObjects;

public sealed class LocalAudioFile : ValueObject
{
    public string Path { get; }
    public string Extension { get; }

    private LocalAudioFile(string path)
    {
        Path = path;
        Extension = System.IO.Path.GetExtension(path).ToUpperInvariant();
    }

    public static LocalAudioFile Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("O caminho do arquivo e obrigatorio.", nameof(path));

        if (!System.IO.Path.IsPathFullyQualified(path))
            throw new ArgumentException("O caminho deve ser absoluto.", nameof(path));

        return new LocalAudioFile(path);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Path;
        yield return Extension;
    }
}