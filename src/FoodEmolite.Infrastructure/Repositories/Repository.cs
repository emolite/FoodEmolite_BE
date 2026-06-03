using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FoodEmolite.Infrastructure.Repositories;

public class Repository<TEntity>
    : IRepository<TEntity>
    where TEntity : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public IQueryable<TEntity> Query()
        => _dbSet.AsQueryable();

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate)
        => await _dbSet.FirstOrDefaultAsync(predicate);

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);

    public async Task AddAsync(TEntity entity)
        => await _dbSet.AddAsync(entity);

    public void Update(TEntity entity)
        => _dbSet.Update(entity);

    public void Remove(TEntity entity)
        => _dbSet.Remove(entity);
}