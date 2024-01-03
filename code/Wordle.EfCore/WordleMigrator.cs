using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Wordle.EfCore;

public class WordleMigrator : IStartable
{
    private readonly WordleEfCoreSettings _context;

    public WordleMigrator(WordleEfCoreSettings context)
    {
        _context = context;
    }

    public void Start()
    {
        _context.DbContext.Database.Migrate();
    }
}