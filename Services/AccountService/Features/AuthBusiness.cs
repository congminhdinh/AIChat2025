using AccountService.Data;
using AccountService.Dtos;
using AccountService.Entities;
using AccountService.Requests;
using AccountService.Specifications;
using Infrastructure;
using Infrastructure.Authentication;
using Infrastructure.Logging;
using Infrastructure.Tenancy;
using Infrastructure.Utils;
using Infrastructure.Web;
using Microsoft.Extensions.Options;

namespace AccountService.Features
{
    public class AuthBusiness : BaseHttpClient
    {
        private readonly IRepository<Account> _repository;
        private readonly ITokenClaimsService _tokenClaimsService;
        private readonly ICurrentTenantProvider _currentTenantProvider;
        private readonly AppSettings _appSettings;

        public AuthBusiness(
            IRepository<Account> repository,
            ITokenClaimsService tokenClaimsService,
            ICurrentTenantProvider currentTenantProvider,
            HttpClient httpClient,
            IAppLogger<BaseHttpClient> appLogger,
            IOptionsMonitor<AppSettings> optionsMonitor)
            : base(httpClient, appLogger)
        {
            _repository = repository;
            _tokenClaimsService = tokenClaimsService;
            _currentTenantProvider = currentTenantProvider;
            _appSettings = optionsMonitor.CurrentValue;
        }
        //public async Task<BaseResponse<TokenDto>> Register(RegisterRequest input, int tenantId)
        //{
        //    _currentTenantProvider.SetTenantId(tenantId);

        //    var isExisted = await _repository.AnyAsync(new AccountSpecification(input.Email, tenantId));
        //    if (isExisted)
        //    {
        //        throw new Exception("Email already exists");
        //    }
        //    var account = new Account(input.Email, input.Password, input.Name, null, tenantId)
        //    {
        //        TenantId = tenantId
        //    };
        //    await _repository.AddAsync(account);
        //    await _repository.SaveChangesAsync();
        //    var token = _tokenClaimsService.GetTokenAsync(tenantId, account.Id, account.Email, AuthorizationConstants.SCOPE_WEB, false);
        //    return new BaseResponse<TokenDto>(new TokenDto(token.AccessToken, token.RefreshToken, token.ExpiresAt), input.CorrelationId());
        //}

        public async Task<BaseResponse<TokenDto>> Login(LoginRequest input, string tenantKey)
        {
            // Validate tenant key and get tenantId from TenantService
            var validateResponse = await PostAsync<string, BaseResponse<int>>(
                $"{_appSettings.ApiGatewayUrl}/web-api/tenant/tenant-key/validate?tenantKey={tenantKey}",
                tenantKey);

            if (validateResponse == null || validateResponse.Data == -1)
            {
                throw new Exception("Invalid tenant key");
            }

            var tenantId = validateResponse.Data;
            _currentTenantProvider.SetTenantId(tenantId);

            var account = await _repository.FirstOrDefaultAsync(new AccountSpecification(input.Email, tenantId));
            if (account == null || !PasswordHasher.VerifyPassword(input.Password, account.Password) || account.TenantId != tenantId)
            {
                throw new Exception("Invalid email or password");
            }
            if (!account.IsActive || !account.TenancyActive)
            {
                throw new Exception("Account is inactive");
            }
            var token = _tokenClaimsService.GetTokenAsync(tenantId, account.Id, account.Email, AuthorizationConstants.SCOPE_WEB, account.IsAdmin);
            return new BaseResponse<TokenDto>(new TokenDto(token.AccessToken, token.RefreshToken, token.ExpiresAt), input.CorrelationId());
        }
    }
}
