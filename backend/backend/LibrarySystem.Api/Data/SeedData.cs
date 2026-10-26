using LibrarySystem.Api.Models;
using LibrarySystem.Api.Services;

namespace LibrarySystem.Api.Data;

public static class SeedData
{
    /// <summary>
    /// Seeds a demo admin (admin@library.local / Admin123!), a demo user
    /// (user@library.local / User123!), and a handful of books, if the
    /// database is empty. Safe to call on every startup.
    /// </summary>
    public static void Seed(LibraryDbContext db, IPasswordHasher hasher)
    {
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User
                {
                    FullName = "Admin User",
                    Email = "admin@library.local",
                    PasswordHash = hasher.Hash("Admin123!"),
                    Role = UserRoles.Admin
                },
                new User
                {
                    FullName = "Demo Reader",
                    Email = "user@library.local",
                    PasswordHash = hasher.Hash("User123!"),
                    Role = UserRoles.User
                }
            );
        }

        if (!db.Books.Any())
        {
            db.Books.AddRange(
                new Book { Title = "Clean Code", Author = "Robert C. Martin", Category = "Software Engineering", TotalCopies = 3, AvailableCopies = 3 },
                new Book { Title = "The Pragmatic Programmer", Author = "Andrew Hunt", Category = "Software Engineering", TotalCopies = 2, AvailableCopies = 2 },
                new Book { Title = "Operating System Concepts", Author = "Silberschatz", Category = "Computer Science", TotalCopies = 4, AvailableCopies = 4 },
                new Book { Title = "Computer Organization and Design", Author = "Patterson & Hennessy", Category = "Computer Science", TotalCopies = 2, AvailableCopies = 2 }
            );
        }

        db.SaveChanges();
    }
}
