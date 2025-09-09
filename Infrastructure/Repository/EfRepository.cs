using Infrastructure.Database;
using Ardalis.Specification.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class EfRepository<T>: RepositoryBase<T>, IRepository<T>, IReadRepository<T> where T : class
    {
        public EfRepository(BaseDbContext dbContext): base(dbContext)
        {
            
        }
    }
}
