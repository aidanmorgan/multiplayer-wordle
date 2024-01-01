using Microsoft.EntityFrameworkCore;
using Wordle.Model;

namespace Wordle.EntityFramework;

public class WordleContext : DbContext
{
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Round> Rounds { get; set; }
    public DbSet<Guess> Guesses { get; set; }
    public DbSet<Options> Options { get; set; }

    private readonly string _connectionString;

    public WordleContext()
    {
        
    }
    
    public WordleContext(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(@"Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase")
            .UseLowerCaseNamingConvention();
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}