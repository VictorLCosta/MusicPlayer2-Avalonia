using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence.Configurations;

public sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Tracks");

        builder.HasKey(track => track.Id);
        builder.Property(track => track.Id).ValueGeneratedNever();
        builder.Property(track => track.Title).HasMaxLength(512).IsRequired();
        builder.Property(track => track.Duration)
            .HasConversion(duration => duration.Ticks, ticks => TimeSpan.FromTicks(ticks));
        builder.Property(track => track.Genre).HasMaxLength(128);
        builder.Property(track => track.SourceFileSizeBytes);
        builder.Property(track => track.SourceLastWriteTimeUtc);

        builder.HasOne(track => track.Artist)
            .WithMany(artist => artist.Tracks)
            .HasForeignKey(track => track.ArtistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(track => track.Album)
            .WithMany()
            .HasForeignKey(track => track.AlbumId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ComplexProperty(track => track.Source, source =>
        {
            source.IsRequired();
            source.Property(file => file.Path).HasColumnName("SourcePath").HasMaxLength(2048).IsRequired();
            source.Ignore(file => file.Extension);
        });

        builder.ComplexProperty(track => track.Position, position =>
        {
            position.Property(value => value.DiscNumber).HasColumnName("DiscNumber");
            position.Property(value => value.TrackNumber).HasColumnName("TrackNumber");
        });

        builder.ComplexProperty(track => track.AudioProperties, audio =>
        {
            audio.IsRequired();
            audio.Property(properties => properties.Duration)
                .HasColumnName("AudioDurationTicks")
                .HasConversion(duration => duration.Ticks, ticks => TimeSpan.FromTicks(ticks));
            audio.Property(properties => properties.BitrateKbps).HasColumnName("BitrateKbps");
            audio.Property(properties => properties.SampleRateHz).HasColumnName("SampleRateHz");
            audio.Property(properties => properties.Channels).HasColumnName("Channels");
            audio.Property(properties => properties.BitDepth).HasColumnName("BitDepth");
        });

        builder.HasIndex(track => track.Title);
        builder.HasIndex(track => track.ArtistId);
        builder.HasIndex(track => track.AlbumId);
    }
}
