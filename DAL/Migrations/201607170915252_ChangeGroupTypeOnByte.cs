namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeGroupTypeOnByte : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Groups", "GroupType", c => c.Byte(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Groups", "GroupType", c => c.Int(nullable: false));
        }
    }
}
