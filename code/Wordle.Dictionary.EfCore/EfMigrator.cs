using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Wordle.Dictionary.EfCore;

public class EfMigrator : IStartable
{
    private DictionaryEfCoreSettings _context;

    public EfMigrator(DictionaryEfCoreSettings context)
    {
        _context = context;
    }

    public void Start()
    {
        _context.Context.Database.Migrate();
    }
}