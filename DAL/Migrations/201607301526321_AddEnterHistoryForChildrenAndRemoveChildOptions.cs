namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEnterHistoryForChildrenAndRemoveChildOptions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EnterChildHistory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ExpulsionNote = c.String(maxLength: 64),
                        ChildId = c.Int(nullable: false),
                        EnterDate = c.DateTime(nullable: false),
                        ExpulsionDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Children", t => t.ChildId, cascadeDelete: true)
                .Index(t => t.ChildId);
            
            AddColumn("dbo.Children", "IsNobody", c => c.Boolean(nullable: false));
            DropColumn("dbo.Children", "EnterDate");
            DropColumn("dbo.Children", "Options");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Children", "Options", c => c.Int(nullable: false));
            AddColumn("dbo.Children", "EnterDate", c => c.DateTime(nullable: false));
            DropForeignKey("dbo.EnterChildHistory", "ChildId", "dbo.Children");
            DropIndex("dbo.EnterChildHistory", new[] { "ChildId" });
            DropColumn("dbo.Children", "IsNobody");
            DropTable("dbo.EnterChildHistory");
        }
    }
}
