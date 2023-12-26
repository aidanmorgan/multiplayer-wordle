namespace Wordle.Events;

public class NewTenant: IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid TenantId { get; set; }
    public string TenantType { get; set; }
    public string TenantName { get; set; }

    public NewTenant(Guid tenantId, string tenantType, string tenantName)
    {
        TenantId = tenantId;
        TenantType = tenantType;
        TenantName = tenantName;
    }
}