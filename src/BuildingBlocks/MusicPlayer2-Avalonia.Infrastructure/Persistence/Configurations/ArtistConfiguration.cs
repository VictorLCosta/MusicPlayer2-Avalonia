using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence.Configurations;

public sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Artists");

        builder.HasKey(artist => artist.Id);
        builder.Property(artist => artist.Id).ValueGeneratedNever();
        builder.Property(artist => artist.Name).HasMaxLength(256).IsRequired();

        builder.HasIndex(artist => artist.Name);
    }
}