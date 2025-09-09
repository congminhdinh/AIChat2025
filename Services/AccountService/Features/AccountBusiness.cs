using AccountService.Data;
using AccountService.Dtos;
using AccountService.Entities;
using Infrastructure.Repository;

namespace AccountService.Features
{
    public class AccountBusiness
    {
        private readonly IRepository<Account> _repository;

        public AccountBusiness(IRepository<Account> repository)
        {
            _repository = repository;
        }

        //Đăng ký tài khoản
        public async Task<TokenDto> Register()
    }
}
