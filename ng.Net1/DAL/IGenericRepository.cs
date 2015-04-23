using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
namespace StockMgr.DAL
{
    public interface IGenericRepository<TEntity>
     where TEntity : class
    {
        void Add(TEntity entity);
        void AddRange(IList<TEntity> entity);
        void Delete(object id);
        void Delete(TEntity entityToDelete);
        IEnumerable<TEntity> Get(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, string includeProperties = "", bool local = false);
        TEntity GetByID(object id);
        IEnumerable<TEntity> GetWithRawSql(string query, params object[] parameters);
        void Update(TEntity entityToUpdate);
    }
}
