using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;

namespace DocumentService.Data
{
    public class EfRepository<T> : RepositoryBase<T>, IRepository<T>, IReadRepository<T> where T : class
    {
        public EfRepository(DocumentDbContext dbContext) : base(dbContext)
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
