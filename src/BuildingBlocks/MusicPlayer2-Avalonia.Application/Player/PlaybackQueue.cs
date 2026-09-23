namespace MusicPlayer2_Avalonia.Application.Player;

public sealed class PlaybackQueue
{
    private readonly List<Guid> _trackIds = [];
    private int _currentIndex = -1;
    public long Revision { get; private set; }

    public IReadOnlyList<Guid> TrackIds => _trackIds;

    public Guid? CurrentTrackId => _currentIndex >= 0 && _currentIndex < _trackIds.Count
        ? _trackIds[_currentIndex]
        : null;

    public Guid? Peek(int offset)
    {
        var index = (long)_currentIndex + offset;
        return _currentIndex >= 0 && index >= 0 && index < _trackIds.Count ? _trackIds[(int)index] : null;
    }

    public void Enqueue(Guid trackId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(trackId, Guid.Empty);
        _trackIds.Add(trackId);
        Revision++;
    }

    public void Replace(IEnumerable<Guid> trackIds)
    {
        ArgumentNullException.ThrowIfNull(trackIds);

        Clear();
        foreach (var trackId in trackIds)
        {
            Enqueue(trackId);
        }
    }

    public void SetCurrent(Guid trackId)
    {
        var index = _trackIds.FindIndex(item => item == trackId);

        if (index < 0)
        {
            Enqueue(trackId);
            index = _trackIds.Count - 1;
        }

        _currentIndex = index;
        Revision++;
    }

    public bool TryMoveNext(out Guid trackId)
    {
        if (_currentIndex >= _trackIds.Count - 1)
        {
            trackId = default;
            return false;
        }

        trackId = _trackIds[++_currentIndex];
        Revision++;
        return true;
    }

    public bool TryMovePrevious(out Guid trackId)
    {
        if (_currentIndex <= 0)
        {
            trackId = default;
            return false;
        }

        trackId = _trackIds[--_currentIndex];
        Revision++;
        return true;
    }

    public void Clear()
    {
        _trackIds.Clear();
        _currentIndex = -1;
        Revision++;
    }
}
