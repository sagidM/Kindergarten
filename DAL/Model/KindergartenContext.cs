using System.Data.Entity;

namespace DAL.Model
{
    public class KindergartenContext : DbContext
    {
        // Your context has been configured to use a 'KindergartenContext' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'DAL.KindergartenContext' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'KindergartenContext' 
        // connection string in the application configuration file.
        public KindergartenContext()
            : base("name=KindergartenContext")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // change foreign key on Children.Id (ref People.Id) and disable cascade delete
            modelBuilder.Entity<Child>()
                .HasRequired(c => c.Person)
                .WithRequiredDependent()
                .WillCascadeOnDelete(false);

            // change foreign key on Parents.Id (ref People.Id) and disable cascade delete
            modelBuilder.Entity<Parent>()
                .HasRequired(c => c.Person)
                .WithRequiredDependent()
                .WillCascadeOnDelete(false);


            // disable cascade delete from Children.GroupId
            modelBuilder.Entity<Child>()
                .HasRequired(c => c.Group)
                .WithMany(g => g.Children)
                .HasForeignKey(c => c.GroupId)
                .WillCascadeOnDelete(false);

            // disable cascade delete from Children.TarifId
            modelBuilder.Entity<Child>()
                .HasRequired(c => c.Tarif)
                .WithMany(t => t.Children)
                .HasForeignKey(c => c.TarifId)
                .WillCascadeOnDelete(false);
        }


        public virtual DbSet<Person> People { get; set; }
        public virtual DbSet<Child> Children { get; set; }
        public virtual DbSet<Parent> Parents { get; set; }
        public virtual DbSet<Group> Groups { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }
        public virtual DbSet<Tarif> Tarifs { get; set; }
        public virtual DbSet<ParentChild> ParentChildren { get; set; }
        public virtual DbSet<EnterChild> EnterChildren { get; set; }
    }
}