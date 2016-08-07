namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePhotoPathColumnFromGroup : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Groups", "PhotoPath");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Groups", "PhotoPath", c => c.String(maxLength: 256));
        }
    }
}
