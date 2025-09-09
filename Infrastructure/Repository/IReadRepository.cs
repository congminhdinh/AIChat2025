using Ardalis.Specification;

namespace Infrastructure.Repository
{
    public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class
    {
    }
}
