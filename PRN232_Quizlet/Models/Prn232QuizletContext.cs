using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace PRN232_Quizlet.Models;

public partial class Prn232QuizletContext : DbContext
{
    public Prn232QuizletContext()
    {
    }

    public Prn232QuizletContext(DbContextOptions<Prn232QuizletContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Flashcard> Flashcards { get; set; }

    public virtual DbSet<FlashcardSet> FlashcardSets { get; set; }

    public virtual DbSet<QuizHistory> QuizHistories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string is configured in Program.cs via appsettings.json
        // No need to configure here if using dependency injection
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flashcard>(entity =>
        {
            entity.HasKey(e => e.FlashcardId).HasName("PK__Flashcar__D36F85529537D066");

            entity.Property(e => e.FlashcardId).HasColumnName("FlashcardID");
            entity.Property(e => e.CorrectOption)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OptionA).HasMaxLength(255);
            entity.Property(e => e.OptionB).HasMaxLength(255);
            entity.Property(e => e.OptionC).HasMaxLength(255);
            entity.Property(e => e.OptionD).HasMaxLength(255);
            entity.Property(e => e.Question).HasMaxLength(500);
            entity.Property(e => e.SetId).HasColumnName("SetID");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Set).WithMany(p => p.Flashcards)
                .HasForeignKey(d => d.SetId)
                .HasConstraintName("FK_Flashcards_FlashcardSets");
        });

        modelBuilder.Entity<FlashcardSet>(entity =>
        {
            entity.HasKey(e => e.SetId).HasName("PK__Flashcar__7E08473D7AEBD933");

            entity.Property(e => e.SetId).HasColumnName("SetID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.StudyCount).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.FlashcardSets)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FlashcardSets_Users");
        });

        modelBuilder.Entity<QuizHistory>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("PK__QuizHist__8B42AE6E0838ADBC");

            entity.ToTable("QuizHistory");

            entity.Property(e => e.QuizId).HasColumnName("QuizID");
            entity.Property(e => e.SetId).HasColumnName("SetID");
            entity.Property(e => e.StartTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Set).WithMany(p => p.QuizHistories)
                .HasForeignKey(d => d.SetId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_QuizHistory_FlashcardSets");

            entity.HasOne(d => d.User).WithMany(p => p.QuizHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_QuizHistory_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACD91820C3");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534BF92DE5A").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("User");
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
