using Infrastructure;

namespace AdminCMS.Models.Tenant
{
    public class DeactivateTenantRequest : BaseRequest
    {
        public int Id { get; set; }
    }
}
