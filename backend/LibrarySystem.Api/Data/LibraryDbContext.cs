using LibrarySystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<User> Users => Set<User>();
    public DbSet<BorrowingRequest> BorrowingRequests => Set<BorrowingRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired().HasMaxLength(300);
            b.Property(x => x.Author).IsRequired().HasMaxLength(200);
            b.Property(x => x.Isbn).HasMaxLength(20);
            b.Property(x => x.Category).HasMaxLength(100);
            b.HasIndex(x => x.Title);
        });

        modelBuilder.Entity<User>(u =>
        {
            u.HasKey(x => x.Id);
            u.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            u.Property(x => x.Email).IsRequired().HasMaxLength(256);
            u.Property(x => x.Role).IsRequired().HasMaxLength(20);
            u.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<BorrowingRequest>(r =>
        {
            r.HasKey(x => x.Id);
            r.Property(x => x.Status).IsRequired().HasMaxLength(20);
            r.Property(x => x.BookTitle).IsRequired().HasMaxLength(300);
            r.HasOne(x => x.Book)
                .WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            r.HasIndex(x => x.Status);
            r.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<Notification>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Type).IsRequired().HasMaxLength(50);
            n.Property(x => x.Title).IsRequired().HasMaxLength(200);
            n.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            n.HasIndex(x => new { x.RecipientUserId, x.IsRead });
        });
    }
}
