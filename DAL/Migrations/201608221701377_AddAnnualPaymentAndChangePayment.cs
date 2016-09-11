namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAnnualPaymentAndChangePayment : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.PaymentHistory", newName: "MonthlyPayments");
            CreateTable(
                "dbo.RangePayments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PaymentFrom = c.DateTime(nullable: false),
                        PaymentTo = c.DateTime(nullable: false),
                        ChildId = c.Int(nullable: false),
                        PaymentDate = c.DateTime(nullable: false),
                        PaidMoney = c.Double(nullable: false),
                        HadToPayMoney = c.Double(nullable: false),
                        Description = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Children", t => t.ChildId, cascadeDelete: true)
                .Index(t => t.ChildId);
            
            AddColumn("dbo.MonthlyPayments", "PayDayCount", c => c.Int());
            AddColumn("dbo.MonthlyPayments", "MonthDayCount", c => c.Int());
            AddColumn("dbo.MonthlyPayments", "HadToPayMoney", c => c.Double(nullable: false));
            AddColumn("dbo.MonthlyPayments", "Description", c => c.String(maxLength: 255));
            DropColumn("dbo.MonthlyPayments", "Debit");
        }
        
        public override void Down()
        {
            AddColumn("dbo.MonthlyPayments", "Debit", c => c.Double(nullable: false));
            DropForeignKey("dbo.RangePayments", "ChildId", "dbo.Children");
            DropIndex("dbo.RangePayments", new[] { "ChildId" });
            DropColumn("dbo.MonthlyPayments", "Description");
            DropColumn("dbo.MonthlyPayments", "HadToPayMoney");
            DropColumn("dbo.MonthlyPayments", "MonthDayCount");
            DropColumn("dbo.MonthlyPayments", "PayDayCount");
            DropTable("dbo.RangePayments");
            RenameTable(name: "dbo.MonthlyPayments", newName: "PaymentHistory");
        }
    }
}
