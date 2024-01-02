using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Wordle.Dictionary.EfCore;

public class EfMigrator : IStartable
{
    private DictionaryContext _context;

    public EfMigrator(DictionaryContext context)
    {
        _context = context;
    }

    public void Start()
    {
        _context.Database.Migrate();
    }
}