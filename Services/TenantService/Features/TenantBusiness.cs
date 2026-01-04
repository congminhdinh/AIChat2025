using Infrastructure.Logging;
using Infrastructure.Paging;
using Infrastructure.Web;
using TenantService.Data;
using TenantService.Entities;
using TenantService.Requests;
using TenantService.Specifications;

namespace TenantService.Features
{
    public class TenantBusiness: BaseHttpClient
    {
        private readonly IRepository<Tenant> _repository;
        private readonly ICurrentUserProvider _currentUserProvider;
        public TenantBusiness(IRepository<Tenant> repository, ICurrentUserProvider currentUserProvider, HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger): base(httpClient, appLogger)
        {
            _repository = repository;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<BaseResponse<PaginatedList<Tenant>>> GetTenantList(GetTenantListRequest input)
        {
            if (!CheckIsSuperAdmin())
            {
                throw new Exception("Only super admin can access this resource");
            }
            var tenants = await _repository.ListAsync(new TenantListSpec(input.Name, input.PageIndex, input.PageSize));
            var count = await _repository.CountAsync(new TenantListSpec(input.Name)); 
            return new BaseResponse<PaginatedList<Tenant>>(new PaginatedList<Tenant>(tenants, count, input.PageIndex, input.PageSize),  input.CorrelationId());
        }

        public async Task<BaseResponse<int>> CreateTenant(CreateTenantRequest input)
        {
            if (!CheckIsSuperAdmin())
            {
                throw new Exception("Only super admin can access this resource");
            }
            var isExisted = await _repository.AnyAsync(new TenantSpecification(input.Name));
            if (isExisted)
            {
                throw new Exception("Tenant name already exists");
            }
            var tenant = new Tenant(input.Name, input.Description, input.IsActive);
            await _repository.AddAsync(tenant);
            await _repository.SaveChangesAsync();
            //var adminAccountRequest = new
            //{

            //}
            return new BaseResponse<int>(tenant.Id, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> UpdateTenant(UpdateTenantRequest input)
        {
            if (!CheckIsSuperAdmin())
            {
                throw new Exception("Only super admin can access this resource");
            }
            var tenant = await _repository.GetByIdAsync(input.Id);
            if (tenant == null)
            {
                throw new Exception("Tenant doesnt exist");
            }
            tenant.Name = input.Name;
            tenant.Description = input.Description;

            await _repository.UpdateAsync(tenant);
            await _repository.SaveChangesAsync();
            return new BaseResponse<int>(tenant.Id, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> DeactivateTenant(DeactivateTenantRequest input)
        {
            if (!CheckIsSuperAdmin())
            {
                throw new Exception("Only super admin can access this resource");
            }
            var tenant = await _repository.GetByIdAsync(input.Id);
            if (tenant == null)
            {
                throw new Exception("Tenant doesnt exist");
            }
            tenant.IsActive = false;

            await _repository.UpdateAsync(tenant);
            await _repository.SaveChangesAsync();
            await PostWithTokenAsync<int, BaseResponse<int>>($"/web-api/account/tenancy-deactivate?tenantId={input.Id}", input.Id, _currentUserProvider.Token);
            return new BaseResponse<int>(tenant.Id, input.CorrelationId());
        }
        private bool CheckIsSuperAdmin()
        {
            var tenantId = _currentUserProvider.TenantId;
            var isAdmin = _currentUserProvider.IsAdmin;
            return tenantId == 1 && isAdmin;
        }
    }
}
