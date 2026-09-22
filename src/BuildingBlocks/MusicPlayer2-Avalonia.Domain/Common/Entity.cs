namespace MusicPlayer2_Avalonia.Domain.Common;

public abstract class Entity<TKey>
{
    public TKey Id { get; set; } = default!;
}

public abstract class Entity : Entity<Guid>;