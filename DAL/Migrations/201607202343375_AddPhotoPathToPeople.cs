namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPhotoPathToPeople : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.People", "PhotoPath", c => c.String(maxLength: 255));
        }
        
        public override void Down()
        {
            DropColumn("dbo.People", "PhotoPath");
        }
    }
}
