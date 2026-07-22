using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Conversations.Persistence.Conversations;
using Umbraco.AI.Agent.Conversations.Persistence.Projects;
using Umbraco.AI.Core.Configuration;
using Umbraco.Cms.Core;

namespace Umbraco.AI.Agent.Conversations.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI Conversations (conversations, messages, projects, project resources).
/// </summary>
public class UmbracoAIConversationsDbContext : DbContext
{
    /// <summary>Projects table.</summary>
    internal DbSet<AIProjectEntity> Projects { get; set; } = null!;

    /// <summary>Project direct-resource attachments table.</summary>
    internal DbSet<AIProjectResourceEntity> ProjectResources { get; set; } = null!;

    /// <summary>Conversations table.</summary>
    internal DbSet<AIConversationEntity> Conversations { get; set; } = null!;

    /// <summary>Conversation direct-resource attachments table.</summary>
    internal DbSet<AIConversationResourceEntity> ConversationResources { get; set; } = null!;

    /// <summary>Messages table.</summary>
    internal DbSet<AIMessageEntity> Messages { get; set; } = null!;

    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    public UmbracoAIConversationsDbContext(DbContextOptions<UmbracoAIConversationsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// The shared migrations history table name for all Umbraco AI packages.
    /// </summary>
    internal const string MigrationsHistoryTableName = AIConnectionStringResolver.MigrationsHistoryTableName;

    /// <summary>
    /// Configures the EF Core database provider with the correct migrations assembly.
    /// </summary>
    internal static void ConfigureProvider(
        DbContextOptionsBuilder options,
        string? connectionString,
        string? providerName)
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(providerName))
        {
            return;
        }

        switch (providerName)
        {
            case Constants.ProviderNames.SQLServer:
                options.UseSqlServer(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Agent.Conversations.Persistence.SqlServer");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Agent.Conversations.Persistence.Sqlite");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{providerName}' is not supported by Umbraco.AI.Agent.Conversations.");
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AIProjectEntity>(entity =>
        {
            entity.ToTable("umbracoAIConversationsProject");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Instructions);
            entity.Property(e => e.UserKey).IsRequired();
            entity.Property(e => e.ContextIds).HasMaxLength(4000);
            entity.Property(e => e.DateCreated).IsRequired();
            entity.Property(e => e.DateModified).IsRequired();
            entity.Property(e => e.Version).IsRequired().HasDefaultValue(1);

            entity.HasIndex(e => e.UserKey);
        });

        modelBuilder.Entity<AIProjectResourceEntity>(entity =>
        {
            entity.ToTable("umbracoAIConversationsProjectResource");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.ResourceTypeId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.Settings);
            entity.Property(e => e.InjectionMode).IsRequired().HasDefaultValue(0);

            entity.HasIndex(e => e.ProjectId);

            // Project delete cascades to its direct resources.
            entity.HasOne<AIProjectEntity>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIConversationEntity>(entity =>
        {
            entity.ToTable("umbracoAIConversationsConversation");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectId).IsRequired(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UserKey).IsRequired();
            entity.Property(e => e.AgentIdOrAlias).HasMaxLength(255);
            entity.Property(e => e.ProfileId).IsRequired(false);
            entity.Property(e => e.ContextIds).HasMaxLength(4000);
            entity.Property(e => e.IsPinned).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsArchived).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DateCreated).IsRequired();
            entity.Property(e => e.DateModified).IsRequired();
            entity.Property(e => e.LastMessageAt).IsRequired(false);
            entity.Property(e => e.Version).IsRequired().HasDefaultValue(1);

            entity.HasIndex(e => e.UserKey);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.LastMessageAt);

            // Project delete orphans conversations (ProjectId set null) rather than cascade-deleting.
            entity.HasOne<AIProjectEntity>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AIConversationResourceEntity>(entity =>
        {
            entity.ToTable("umbracoAIConversationsConversationResource");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ConversationId).IsRequired();
            entity.Property(e => e.ResourceTypeId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.Settings);
            entity.Property(e => e.InjectionMode).IsRequired().HasDefaultValue(0);

            entity.HasIndex(e => e.ConversationId);

            // Conversation delete cascades to its direct resources.
            entity.HasOne<AIConversationEntity>()
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIMessageEntity>(entity =>
        {
            entity.ToTable("umbracoAIConversationsMessage");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ConversationId).IsRequired();
            entity.Property(e => e.Sequence).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ContentJson).IsRequired();
            entity.Property(e => e.ContentText);
            entity.Property(e => e.SchemaVersion).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.InputTokens).IsRequired(false);
            entity.Property(e => e.OutputTokens).IsRequired(false);
            entity.Property(e => e.DateCreated).IsRequired();

            // Ordering anchor + concurrency guard for server-assigned sequence (interrogation B1).
            entity.HasIndex(e => new { e.ConversationId, e.Sequence }).IsUnique();

            // Conversation delete cascades to its messages.
            entity.HasOne<AIConversationEntity>()
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
