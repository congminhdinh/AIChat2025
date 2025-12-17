using AccountService.Data;
using AccountService.Dtos;
using AccountService.Entities;
using AccountService.Requests;
using AccountService.Specifications;
using Infrastructure;
using Infrastructure.Authentication;
using Infrastructure.Tenancy;
using Infrastructure.Utils;

namespace AccountService.Features
{
    public class AuthBusiness
    {
        private readonly IRepository<Account> _repository;
        private readonly ITokenClaimsService _tokenClaimsService;
        private readonly ICurrentTenantProvider _currentTenantProvider;
        public AuthBusiness(IRepository<Account> repository, ITokenClaimsService tokenClaimsService, ICurrentTenantProvider currentTenantProvider)
        {
            _repository = repository;
            _tokenClaimsService = tokenClaimsService;
            _currentTenantProvider = currentTenantProvider;
        }
        public async Task<BaseResponse<TokenDto>> Register(RegisterRequest input, int tenantId)
        {
            // Set tenant context before any database operations
            _currentTenantProvider.SetTenantId(tenantId);

            var isExisted = await _repository.AnyAsync(new AccountSpecification(input.Email, tenantId));
            if (isExisted)
            {
                throw new Exception("Email already exists");
            }
            var account = new Account(input.Email, input.Password, input.Name, null, tenantId)
            {
                TenantId = tenantId
            };
            await _repository.AddAsync(account);
            await _repository.SaveChangesAsync();
            var token = _tokenClaimsService.GetTokenAsync(tenantId, account.Id, account.Email, AuthorizationConstants.SCOPE_WEB, false);
            return new BaseResponse<TokenDto>(new TokenDto(token.AccessToken, token.RefreshToken, token.ExpiresAt), input.CorrelationId());
        }

        public async Task<BaseResponse<TokenDto>> Login(LoginRequest input, int tenantId)
        {
            // Set tenant context before any database operations
            _currentTenantProvider.SetTenantId(tenantId);

            var account = await _repository.FirstOrDefaultAsync(new AccountSpecification(input.Email, tenantId));
            if (account == null || !PasswordHasher.VerifyPassword(input.Password, account.Password))
            {
                throw new Exception("Invalid email or password");
            }
            var token = _tokenClaimsService.GetTokenAsync(tenantId, account.Id, account.Email, AuthorizationConstants.SCOPE_WEB, account.IsAdmin);
            return new BaseResponse<TokenDto>(new TokenDto(token.AccessToken, token.RefreshToken, token.ExpiresAt), input.CorrelationId());
        }
    }
}
