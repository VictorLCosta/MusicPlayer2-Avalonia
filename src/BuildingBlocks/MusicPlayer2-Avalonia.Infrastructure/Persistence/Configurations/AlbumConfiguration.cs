using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence.Configurations;

public sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Albums");

        builder.HasKey(album => album.Id);
        builder.Property(album => album.Id).ValueGeneratedNever();
        builder.Property(album => album.Title).HasMaxLength(512).IsRequired();

        builder.HasOne(album => album.Artist)
            .WithMany()
            .HasForeignKey(album => album.AlbumArtistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(album => new { album.Title, album.AlbumArtistId });
    }
}
