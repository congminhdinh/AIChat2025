using AccountService.Data;

namespace AccountService.Features
{
    public class Register
    {
        private readonly AccountDbContext _context;
        public Register(AccountDbContext context)
        {
            _context = context;
        }

    }
}
