using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Infrastructure.Database;

namespace TenantService.Data
{
    public class EfRepository<T> : RepositoryBase<T>, IRepository<T>, IReadRepository<T> where T : class
    {
        public EfRepository(AccountDbContext dbContext) : base(dbContext)
        {

        }
    }
    public interface IRepository<T> : IRepositoryBase<T> where T : class
    {
    }
    public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class
    {
    }
}
