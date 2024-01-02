using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Wordle.EfCore;

public class EfMigrator : IStartable
{
    private WordleEfCoreSettings _context;

    public EfMigrator(WordleEfCoreSettings context)
    {
        _context = context;
    }

    public void Start()
    {
        _context.DbContext.Database.Migrate();
    }
}