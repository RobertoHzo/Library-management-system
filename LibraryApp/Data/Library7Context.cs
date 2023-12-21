using LibraryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data
{
    public class Library7Context : DbContext
    {
        public Library7Context(DbContextOptions<Library7Context> options)
        : base(options)
        {
        }

        public DbSet<User> User { get; set; }

        public DbSet<Member> Member { get; set; }

        public DbSet<Role> Role { get; set; }

        public DbSet<Permission> Permission { get; set; }

        public DbSet<Book> Book { get; set; } = default!;

        public DbSet<Section> Section { get; set; } = default!;

        public DbSet<Loan> Loan { get; set; } = default!;

        public DbSet<Fine> Fine { get; set; } = default!;

        public DbSet<SavedGroup> SavedGroup { get; set; } = default!;

        public DbSet<LoanConfiguration> LoanConfiguration { get; set; } = default!;


    }
}
