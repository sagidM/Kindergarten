using System.Linq;

namespace DAL.Repository
{
    // Repository disable!!!


    /*public*/
    internal interface IRepository<T>
    {
        IQueryable<T> Table { get; }

        void Delete(T entity);
        T GetById(object id);
        void Insert(T entity);
        void Update(T entity);
    }
}