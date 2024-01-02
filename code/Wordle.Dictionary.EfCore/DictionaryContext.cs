using Microsoft.EntityFrameworkCore;

namespace Wordle.Dictionary.EfCore;

public class Entry
{
    public long Id { get; set; }
    public string Language { get; set; }
    public int WordLength { get; set; }
    public string Word { get; set; }
}

public class DictionaryContext : DbContext
{
    public DbSet<Entry> Words { get; set; }
    
    private readonly string _connectionString;

    public DictionaryContext()
    {
        
    }
    
    public DictionaryContext(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(_connectionString)
            .UseLowerCaseNamingConvention();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entry = modelBuilder.Entity<Entry>();
        
        entry
            .Property(x => x.Id)
            .HasIdentityOptions(startValue: 1, incrementBy: 1);

        entry.Property(x => x.Language);
        entry.Property(x => x.WordLength);
        entry.Property(x => x.Word);
        
        entry.HasIndex(x => new { x.Language, x.WordLength});
    }
    
    
}