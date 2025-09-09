using Ardalis.Specification;

namespace Infrastructure.Repository
{
    public interface IRepository<T>: IRepositoryBase<T> where T : class
    {
    }
}