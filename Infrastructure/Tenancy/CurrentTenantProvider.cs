namespace Infrastructure.Tenancy
{
    public interface ICurrentTenantProvider
    {
        int TenantId { get; }
        void SetTenantId(int? tenantId);
    }
    public class CurrentTenantProvider: ICurrentTenantProvider
    {
       private int _tenantId;
       public int TenantId => _tenantId;
       public void SetTenantId(int? tenantId) => _tenantId = tenantId ?? 0;
    }
}
