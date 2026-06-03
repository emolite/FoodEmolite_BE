using System.Linq.Expressions;

namespace FoodEmolite.Domain.Interfaces;

public interface IRepository<TEntity>
    where TEntity : class
{
    IQueryable<TEntity> Query();
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate);
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate);
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}