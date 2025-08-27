using DevHabits.Api.HabitTags.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabits.Api.HabitTags.Configurations;

public sealed class HabitTagsConfigurations : IEntityTypeConfiguration<HabitTag> {
    public void Configure(EntityTypeBuilder<HabitTag> builder) {
        builder.HasKey(ht => new { ht.HabitId, ht.TagId });

        builder.Property(p => p.HabitId).HasMaxLength(500);
        builder.Property(p => p.TagId).HasMaxLength(500);

        builder.HasOne(ht => ht.Habit)
            .WithMany(h => h.HabitTags)
            .HasForeignKey(ht => ht.HabitId);

        builder.HasOne(ht => ht.Tag)
            .WithMany(t => t.HabitTags)
            .HasForeignKey(ht => ht.TagId);
    }
}
