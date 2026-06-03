using FoodEmolite.Domain.Entities;

namespace FoodEmolite.Domain.Interfaces;

public interface IUnitOfWork
{
    IRepository<T> GetRepository<T>() where T : class;
    Task<int> SaveChangesAsync();
}