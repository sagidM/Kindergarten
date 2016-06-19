using System.Data.Entity;
using System.Linq;
using DAL.Model;

namespace DAL.Repository
{
    internal class Repository<T> : IRepository<T> where T : BaseEntity<int>
    {
        private readonly KindergartenContext _context;
        private IDbSet<T> _entities;

        private IDbSet<T> Entities => _entities ?? (_entities = _context.Set<T>());
        public IQueryable<T> Table => Entities;


        public Repository(KindergartenContext context)
        {
            _context = context;
        }


        public T GetById(object id)
        {
            return Entities.Find(id);
        }

        public void Insert(T entity)
        {
            entity.NotNull();
            Entities.Add(entity);
            _context.SaveChanges();
        }

        public void Update(T entity)
        {
            entity.NotNull();
            _context.SaveChanges();
        }
        public void Delete(T entity)
        {
            entity.NotNull();
            _entities.Remove(entity);
            _context.SaveChanges();
        }
    }
}