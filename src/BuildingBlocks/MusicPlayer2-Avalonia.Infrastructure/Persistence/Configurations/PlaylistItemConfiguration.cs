using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence.Configurations;

public sealed class PlaylistItemConfiguration : IEntityTypeConfiguration<PlaylistItem>
{
    public void Configure(EntityTypeBuilder<PlaylistItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PlaylistItems");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Position).IsRequired();
        builder.Property(item => item.AddedAt).IsRequired();

        builder.HasOne<Track>()
            .WithMany()
            .HasForeignKey(item => item.TrackId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("PlaylistId", nameof(PlaylistItem.Position)).IsUnique();
        builder.HasIndex(item => item.TrackId);
    }
}