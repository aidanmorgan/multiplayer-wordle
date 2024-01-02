using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Wordle.EfCore;

public class EfMigrator : IStartable
{
    private WordleContext _context;

    public EfMigrator(WordleContext context)
    {
        _context = context;
    }

    public void Start()
    {
        _context.Database.Migrate();
    }
}