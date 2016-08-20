namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSexToParent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Parents", "Sex", c => c.Byte(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Parents", "Sex");
        }
    }
}
