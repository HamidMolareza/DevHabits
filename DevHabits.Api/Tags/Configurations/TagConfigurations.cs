using DevHabits.Api.Tags.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabits.Api.Tags.Configurations;

public sealed class TagConfigurations : IEntityTypeConfiguration<Tag> {
    public void Configure(EntityTypeBuilder<Tag> builder) {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).IsRequired().HasMaxLength(500);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);

        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasIndex(t => new { t.Name }).IsUnique();

        builder.HasMany(t => t.HabitTags)
            .WithOne(ht => ht.Tag)
            .HasForeignKey(ht => ht.TagId);
    }
}
