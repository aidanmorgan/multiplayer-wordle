using MediatR;
using Wordle.Model;

namespace Wordle.Commands;

public class CreateNewSessionCommand : IRequest<Guid>
{
    public string TenantType { get; private set; }
    public string TenantName { get; private set; }
    public string Word { get; private set; }
    
    public Options Options { get; private set; }

    public CreateNewSessionCommand(string tenantType, string tenantName, string word = null, Options o = null)
    {
        TenantType = tenantType;
        TenantName = tenantName;
        Word = word;
        Options = o;
    }
}