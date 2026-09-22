namespace MusicPlayer2_Avalonia.Application.Player;

public sealed class PlaybackQueue
{
    private readonly List<Guid> _trackIds = [];
    private int _currentIndex = -1;

    public IReadOnlyList<Guid> TrackIds => _trackIds;

    public Guid? CurrentTrackId => _currentIndex >= 0 && _currentIndex < _trackIds.Count
        ? _trackIds[_currentIndex]
        : null;

    public void Enqueue(Guid trackId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(trackId, Guid.Empty);
        _trackIds.Add(trackId);
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
    }

    public bool TryMoveNext(out Guid trackId)
    {
        if (_currentIndex >= _trackIds.Count - 1)
        {
            trackId = default;
            return false;
        }

        trackId = _trackIds[++_currentIndex];
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
        return true;
    }

    public void Clear()
    {
        _trackIds.Clear();
        _currentIndex = -1;
    }
}