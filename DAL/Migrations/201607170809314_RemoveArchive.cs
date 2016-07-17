namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveArchive : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ChildArchive", "ChildId", "dbo.Children");
            DropIndex("dbo.ChildArchive", new[] { "ChildId" });
            AddColumn("dbo.Children", "IsNobody", c => c.Boolean(nullable: false));
            DropColumn("dbo.Children", "ChildArchiveId");
            DropTable("dbo.ChildArchive");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.ChildArchive",
                c => new
                    {
                        ChildId = c.Int(nullable: false),
                        IsNobody = c.Boolean(nullable: false),
                        AddedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ChildId);
            
            AddColumn("dbo.Children", "ChildArchiveId", c => c.Int());
            DropColumn("dbo.Children", "IsNobody");
            CreateIndex("dbo.ChildArchive", "ChildId");
            AddForeignKey("dbo.ChildArchive", "ChildId", "dbo.Children", "Id");
        }
    }
}
