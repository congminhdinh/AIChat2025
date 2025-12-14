using AccountService.Data;
using AccountService.Dtos;
using AccountService.Entities;
using AccountService.Requests;
using AccountService.Specifications;
using Infrastructure;
using Infrastructure.Paging;
using Infrastructure.Utils;
using Infrastructure.Web;

namespace AccountService.Features
{
    public class AccountBusiness
    {
        private readonly IRepository<Account> _repository;
        private readonly ICurrentUserProvider _currentUserProvider;
        public AccountBusiness(IRepository<Account> repository, ICurrentUserProvider currentUserProvider)
        {
            _repository = repository;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<BaseResponse<AccountDto>> GetAccountById(GetAccountByIdRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var account = await _repository.FirstOrDefaultAsync(new AccountSpecificationById(input.AccountId, tenantId));
            if (account == null)
            {
                throw new Exception("Account not found");
            }
            var accountDto = new AccountDto(account.Id, account.Email, account.Name, account.Avatar, account.IsAdmin, account.IsActive);
            return new BaseResponse<AccountDto>(accountDto, input.CorrelationId());
        }

        public async Task<BaseResponse<PaginatedList<AccountDto>>> GetAccountList(GetAccountListRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var spec = new AccountListSpec(tenantId, input.Name, input.Username, input.Email, input.pageIndex, input.pageSize);
            var accounts = await _repository.ListAsync(spec);
            var count = accounts.Count;

            var accountDtos = accounts.Select(account => new AccountDto(account.Id, account.Email, account.Name, account.Avatar, account.IsAdmin, account.IsActive)).ToList();
            return new BaseResponse<PaginatedList<AccountDto>>(new PaginatedList<AccountDto>(accountDtos, count, input.pageIndex, input.pageSize), input.CorrelationId());
        }

        public async Task<BaseResponse<int>> CreateAccount(CreateAccountRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var account = await _repository.FirstOrDefaultAsync(new AccountSpecification(input.Email, tenantId));
            if(account != null)
            {
                throw new Exception("Account with this email already exists");
            }
            var newAccount = new Account(input.Email, input.Password, input.Name, null); ///123456 is default password
            await _repository.AddAsync(newAccount);
            return new BaseResponse<int>(newAccount.Id, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> ChangePassword(ChangePasswordRequest input)
        {
            var tenantId = _currentUserProvider.TenantId;
            var account = await _repository.FirstOrDefaultAsync(new AccountSpecificationById(input.AccountId, tenantId));
            if (account == null)
            {
                throw new Exception("Account not found");
            }
            account.Password = PasswordHasher.HashPassword(input.NewPassword);
            await _repository.UpdateAsync(account);
            return new BaseResponse<int>(account.Id, input.CorrelationId());
        }

        public async Task<BaseResponse<int>> UpdateAccount(UpdateAccountRequest input)  
        {
            var tenantId = _currentUserProvider.TenantId;
            var account = await _repository.FirstOrDefaultAsync(new AccountSpecificationById(input.AccountId, tenantId));
            if (account == null)
            {
                throw new Exception("Account not found");
            }
            account.Name = input.Name;
            account.IsActive = input.IsActive;
            account.Avatar = input.Avatar;
            await _repository.UpdateAsync(account);
            return new BaseResponse<int>(account.Id, input.CorrelationId());
        }
    }
}
