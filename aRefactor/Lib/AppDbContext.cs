using Microsoft.EntityFrameworkCore;
using aRefactor.Domain.Model;

namespace aRefactor.Lib;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Pattern> Patterns { get; set; }
    public DbSet<Implementation> Implementations { get; set; }
    public DbSet<ImplementationFile> ImplementationFiles { get; set; }
    public DbSet<RefactorExample> RefactorExamples { get; set; }
    public DbSet<RefactorSnippet> RefactorSnippets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<Pattern>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Summary).IsRequired();
            entity.Property(e => e.Problem).IsRequired();
            entity.Property(e => e.Solution).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Patterns)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Implementation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).IsRequired();

            entity.HasOne(e => e.Pattern)
                .WithMany(p => p.Implementations)
                .HasForeignKey(e => e.PatternId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImplementationFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CodeBlock).IsRequired();
            entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Implementation)
                .WithMany(i => i.Files)
                .HasForeignKey(e => e.ImplementationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefactorExample>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Problem).IsRequired();

            entity.HasOne(e => e.Pattern)
                .WithMany(p => p.RefactorExamples)
                .HasForeignKey(e => e.PatternId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefactorSnippet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.CodeBlock).IsRequired();
            entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.RefactorExample)
                .WithMany(r => r.Snippets)
                .HasForeignKey(e => e.RefactorExampleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}