using MediatR;
using Wordle.Model;

namespace Wordle.Commands;

public class CreateNewSessionCommand : IRequest<Guid>
{
    public string TenantName { get; private set; }
    public string Word { get; private set; }
    
    public Options Options { get; private set; }

    public CreateNewSessionCommand(string tenantName, string word = null, Options o = null)
    {
        TenantName = tenantName;
        Word = word;
        Options = o;
    }
}