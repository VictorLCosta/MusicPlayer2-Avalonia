using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer2_Avalonia.Models;

/// <summary>
/// Represents an item in the library navigation tree.
/// </summary>
internal sealed partial class LibraryNode : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public required LibraryNodeKind Kind { get; init; }

    /// <summary>
    /// Identifies the artist, album or playlist represented by this node.
    /// Group and folder nodes do not require an entity ID.
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Directory represented by a folder node.
    /// </summary>
    public string? FolderPath { get; init; }

    public ObservableCollection<LibraryNode> Children { get; } = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
}