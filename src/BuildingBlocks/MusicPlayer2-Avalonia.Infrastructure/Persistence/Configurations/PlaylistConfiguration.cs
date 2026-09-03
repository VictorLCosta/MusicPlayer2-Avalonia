using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence.Configurations;

public sealed class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Playlists");

        builder.HasKey(playlist => playlist.Id);
        builder.Property(playlist => playlist.Id).ValueGeneratedNever();
        builder.Property(playlist => playlist.Name).HasMaxLength(256).IsRequired();

        builder.HasMany(playlist => playlist.PlaylistItems)
            .WithOne()
            .HasForeignKey("PlaylistId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(playlist => playlist.PlaylistItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
