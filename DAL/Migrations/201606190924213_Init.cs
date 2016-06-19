namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Children",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        GroupId = c.Int(nullable: false),
                        LocationAddress = c.String(nullable: false, maxLength: 128),
                        BirthDate = c.DateTime(nullable: false),
                        Sex = c.Byte(nullable: false),
                        EnterDate = c.DateTime(nullable: false),
                        ChildArchiveId = c.Int(),
                        PaymentSystem = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Groups", t => t.GroupId)
                .ForeignKey("dbo.People", t => t.Id)
                .Index(t => t.Id)
                .Index(t => t.GroupId);
            
            CreateTable(
                "dbo.ChildArchive",
                c => new
                    {
                        ChildId = c.Int(nullable: false),
                        IsNobody = c.Boolean(nullable: false),
                        AddedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ChildId)
                .ForeignKey("dbo.Children", t => t.ChildId)
                .Index(t => t.ChildId);
            
            CreateTable(
                "dbo.Groups",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 64),
                        GroupType = c.Int(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        PhotoPath = c.String(maxLength: 256),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ParentChild",
                c => new
                    {
                        ChildId = c.Int(nullable: false),
                        ParentId = c.Int(nullable: false),
                        ParentType = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => new { t.ChildId, t.ParentId })
                .ForeignKey("dbo.Children", t => t.ChildId, cascadeDelete: true)
                .ForeignKey("dbo.Parents", t => t.ParentId, cascadeDelete: true)
                .Index(t => t.ChildId)
                .Index(t => t.ParentId);
            
            CreateTable(
                "dbo.Parents",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        LocationAddress = c.String(nullable: false, maxLength: 128),
                        ResidenceAddress = c.String(maxLength: 128),
                        WorkAddress = c.String(maxLength: 128),
                        PassportSeries = c.String(maxLength: 10),
                        PassportIssuedBy = c.String(maxLength: 256),
                        PassportIssueDate = c.DateTime(nullable: false),
                        PhoneNumber = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.People", t => t.Id)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.People",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FirstName = c.String(nullable: false, maxLength: 64),
                        LastName = c.String(nullable: false, maxLength: 64),
                        Patronymic = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Payments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ChildId = c.Int(nullable: false),
                        PaymentDate = c.DateTime(nullable: false),
                        PaymentSystem = c.Int(nullable: false),
                        PaidMoney = c.Double(nullable: false),
                        Debit = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Children", t => t.ChildId, cascadeDelete: true)
                .Index(t => t.ChildId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Children", "Id", "dbo.People");
            DropForeignKey("dbo.Payments", "ChildId", "dbo.Children");
            DropForeignKey("dbo.Parents", "Id", "dbo.People");
            DropForeignKey("dbo.ParentChild", "ParentId", "dbo.Parents");
            DropForeignKey("dbo.ParentChild", "ChildId", "dbo.Children");
            DropForeignKey("dbo.Children", "GroupId", "dbo.Groups");
            DropForeignKey("dbo.ChildArchive", "ChildId", "dbo.Children");
            DropIndex("dbo.Payments", new[] { "ChildId" });
            DropIndex("dbo.Parents", new[] { "Id" });
            DropIndex("dbo.ParentChild", new[] { "ParentId" });
            DropIndex("dbo.ParentChild", new[] { "ChildId" });
            DropIndex("dbo.ChildArchive", new[] { "ChildId" });
            DropIndex("dbo.Children", new[] { "GroupId" });
            DropIndex("dbo.Children", new[] { "Id" });
            DropTable("dbo.Payments");
            DropTable("dbo.People");
            DropTable("dbo.Parents");
            DropTable("dbo.ParentChild");
            DropTable("dbo.Groups");
            DropTable("dbo.ChildArchive");
            DropTable("dbo.Children");
        }
    }
}
