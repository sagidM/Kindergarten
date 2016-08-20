namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveParentSex_AddParentChildParentTypeText : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ParentChild", "ParentTypeText", c => c.String(maxLength: 32));
            DropColumn("dbo.Parents", "Sex");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Parents", "Sex", c => c.Byte(nullable: false));
            DropColumn("dbo.ParentChild", "ParentTypeText");
        }
    }
}
