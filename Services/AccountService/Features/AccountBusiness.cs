using AccountService.Data;
using AccountService.Dtos;
using AccountService.Entities;
using AccountService.Specifications;
using Infrastructure.Authentication;
using Infrastructure.Repository;
using Infrastructure.Utils;
using System.Linq.Expressions;

namespace AccountService.Features
{
    public class AccountBusiness
    {
        private readonly IRepository<Account> _repository;
        private readonly ITokenClaimsService _tokenClaimsService;
        public AccountBusiness(IRepository<Account> repository, ITokenClaimsService tokenClaimsService)
        {
            _repository = repository;
            _tokenClaimsService = tokenClaimsService;
        }

        //Đăng ký tài khoản
        public async Task<TokenDto> Register(RegisterDto input, int tenantId)
        {
            var isExisted = await _repository.AnyAsync(new AccountSpecification(input.Email, tenantId));
            if (isExisted)
            {
                throw new Exception("Email already exists");
            }
            var account = new Account(input.Email, input.Password, input.Name, null)
            {
                TenantId = tenantId
            };
            await _repository.AddAsync(account);
            await _repository.SaveChangesAsync();
            var token = _tokenClaimsService.GetTokenAsync(tenantId, account.Id, account.Email, AuthorizationConstants.SCOPE_WEB);
            return new TokenDto(token.AccessToken, token.RefreshToken, token.ExpiresAt);
        }

        public async Task<TokenDto> Login(LoginDto input, int tenantId)
        {
            var account = await _repository.FirstOrDefaultAsync(new AccountSpecification(input.Email, tenantId));
            if (account == null || !PasswordHasher.VerifyPassword(account.Password, input.Password))
            {
                throw new Exception("Invalid email or password");
            }
            var token = _tokenClaimsService.GetTokenAsync(tenantId, account.Id, account.Email, AuthorizationConstants.SCOPE_WEB);
            return new TokenDto(token.AccessToken, token.RefreshToken, token.ExpiresAt);
        }
    }
}
