using Infrastructure.Web;

namespace Infrastructure.Tenancy
{
    public interface ICurrentTenantProvider
    {
        int TenantId { get; }
        void SetTenantId(int? tenantId);
    }

    /// <summary>
    /// Hybrid tenant provider that supports both HTTP context (from JWT claims)
    /// and manual impersonation (for background processes like RabbitMQ consumers).
    /// </summary>
    public class CurrentTenantProvider : ICurrentTenantProvider
    {
        private readonly ICurrentUserProvider? _currentUserProvider;
        private int? _manualTenantId;

        public CurrentTenantProvider(ICurrentUserProvider? currentUserProvider = null)
        {
            _currentUserProvider = currentUserProvider;
        }
        public int TenantId
        {
            get
            {
                if (_currentUserProvider != null)
                {
                    try
                    {
                        var tenantIdFromContext = _currentUserProvider.TenantId;
                        if (tenantIdFromContext > 0)
                        {
                            return tenantIdFromContext;
                        }
                    }
                    catch
                    {
                    }
                }
                return _manualTenantId ?? 0;
            }
        }

        public void SetTenantId(int? tenantId) => _manualTenantId = tenantId;
    }
}
