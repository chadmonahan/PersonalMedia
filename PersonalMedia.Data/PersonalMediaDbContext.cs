using Microsoft.EntityFrameworkCore;
using PersonalMedia.Core.Entities;

namespace PersonalMedia.Data;

public class PersonalMediaDbContext : DbContext
{
    public PersonalMediaDbContext(DbContextOptions<PersonalMediaDbContext> options)
        : base(options)
    {
    }

    public DbSet<MediaSet> MediaSets { get; set; }
    public DbSet<MediaItem> MediaItems { get; set; }
    public DbSet<MediaReaction> MediaReactions { get; set; }
    public DbSet<BasePersonImage> BasePersonImages { get; set; }
    public DbSet<GenerationParameter> GenerationParameters { get; set; }
    public DbSet<ParameterOption> ParameterOptions { get; set; }
    public DbSet<GenerationSettings> GenerationSettings { get; set; }
    public DbSet<RunPodWebhookLog> RunPodWebhookLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MediaSet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            entity.HasMany(e => e.MediaItems)
                .WithOne(e => e.MediaSet)
                .HasForeignKey(e => e.MediaSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MediaType).IsRequired();
            entity.Property(e => e.AzureStorageUrl).HasMaxLength(500);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.GenerationPrompt).HasMaxLength(2000);
            entity.Property(e => e.GenerationStatus).IsRequired().HasDefaultValue(GenerationStatus.Pending);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.RunPodJobId).HasMaxLength(100);
            entity.Property(e => e.RawWebhookPayload).HasMaxLength(4000);

            entity.HasIndex(e => e.RunPodJobId);

            entity.HasOne(e => e.BasePersonImage)
                .WithMany(e => e.MediaItems)
                .HasForeignKey(e => e.BasePersonImageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Reactions)
                .WithOne(e => e.MediaItem)
                .HasForeignKey(e => e.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.GenerationParameters)
                .WithOne(e => e.MediaItem)
                .HasForeignKey(e => e.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReactionType).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();

            entity.HasIndex(e => new { e.MediaItemId, e.ReactionType });
        });

        modelBuilder.Entity<BasePersonImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AzureStorageUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.UsageCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<GenerationParameter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Value).IsRequired().HasMaxLength(200);

            entity.HasIndex(e => new { e.MediaItemId, e.Category });
        });

        modelBuilder.Entity<ParameterOption>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Value).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.Weight).HasDefaultValue(1);

            entity.HasIndex(e => new { e.Category, e.IsActive });
        });

        modelBuilder.Entity<GenerationSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DailySetsCount).IsRequired().HasDefaultValue(5);
            entity.Property(e => e.ImagesPerSetMin).IsRequired().HasDefaultValue(3);
            entity.Property(e => e.ImagesPerSetMax).IsRequired().HasDefaultValue(5);
            entity.Property(e => e.MaxRetryAttempts).IsRequired().HasDefaultValue(3);
            entity.Property(e => e.ModestyLevel).HasMaxLength(100).HasDefaultValue("Family Friendly");
            entity.Property(e => e.ModifiedDate).IsRequired();
        });

        modelBuilder.Entity<RunPodWebhookLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReceivedDate).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.RawPayload).HasMaxLength(4000);
            entity.Property(e => e.ProcessingError).HasMaxLength(1000);

            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.ReceivedDate);
        });

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GenerationSettings>().HasData(
            new GenerationSettings
            {
                Id = 1,
                DailySetsCount = 5,
                ImagesPerSetMin = 3,
                ImagesPerSetMax = 5,
                MaxRetryAttempts = 3,
                ModestyLevel = "Family Friendly",
                LastGenerationDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        var parameterOptions = new List<ParameterOption>
        {
            new() { Id = 1, Category = ParameterCategory.Setting, Value = "Beach", IsActive = true, Weight = 1 },
            new() { Id = 2, Category = ParameterCategory.Setting, Value = "Mountain", IsActive = true, Weight = 1 },
            new() { Id = 3, Category = ParameterCategory.Setting, Value = "City", IsActive = true, Weight = 1 },
            new() { Id = 4, Category = ParameterCategory.Setting, Value = "Forest", IsActive = true, Weight = 1 },
            new() { Id = 5, Category = ParameterCategory.Setting, Value = "Park", IsActive = true, Weight = 1 },

            new() { Id = 6, Category = ParameterCategory.Mood, Value = "Happy", IsActive = true, Weight = 1 },
            new() { Id = 7, Category = ParameterCategory.Mood, Value = "Peaceful", IsActive = true, Weight = 1 },
            new() { Id = 8, Category = ParameterCategory.Mood, Value = "Excited", IsActive = true, Weight = 1 },
            new() { Id = 9, Category = ParameterCategory.Mood, Value = "Contemplative", IsActive = true, Weight = 1 },

            new() { Id = 10, Category = ParameterCategory.Activity, Value = "Reading", IsActive = true, Weight = 1 },
            new() { Id = 11, Category = ParameterCategory.Activity, Value = "Walking", IsActive = true, Weight = 1 },
            new() { Id = 12, Category = ParameterCategory.Activity, Value = "Picnic", IsActive = true, Weight = 1 },
            new() { Id = 13, Category = ParameterCategory.Activity, Value = "Photography", IsActive = true, Weight = 1 },
            new() { Id = 14, Category = ParameterCategory.Activity, Value = "Relaxing", IsActive = true, Weight = 1 },

            new() { Id = 15, Category = ParameterCategory.Clothing, Value = "Casual", IsActive = true, Weight = 1 },
            new() { Id = 16, Category = ParameterCategory.Clothing, Value = "Athletic", IsActive = true, Weight = 1 },
            new() { Id = 17, Category = ParameterCategory.Clothing, Value = "Smart Casual", IsActive = true, Weight = 1 },

            new() { Id = 18, Category = ParameterCategory.TimeOfDay, Value = "Morning", IsActive = true, Weight = 1 },
            new() { Id = 19, Category = ParameterCategory.TimeOfDay, Value = "Afternoon", IsActive = true, Weight = 1 },
            new() { Id = 20, Category = ParameterCategory.TimeOfDay, Value = "Evening", IsActive = true, Weight = 1 },
            new() { Id = 21, Category = ParameterCategory.TimeOfDay, Value = "Golden Hour", IsActive = true, Weight = 1 },

            new() { Id = 22, Category = ParameterCategory.Weather, Value = "Sunny", IsActive = true, Weight = 1 },
            new() { Id = 23, Category = ParameterCategory.Weather, Value = "Cloudy", IsActive = true, Weight = 1 },
            new() { Id = 24, Category = ParameterCategory.Weather, Value = "Partly Cloudy", IsActive = true, Weight = 1 },

            new() { Id = 25, Category = ParameterCategory.Style, Value = "Photorealistic", IsActive = true, Weight = 1 },
            new() { Id = 26, Category = ParameterCategory.Style, Value = "Cinematic", IsActive = true, Weight = 1 },
            new() { Id = 27, Category = ParameterCategory.Style, Value = "Portrait", IsActive = true, Weight = 1 }
        };

        modelBuilder.Entity<ParameterOption>().HasData(parameterOptions);
    }
}
