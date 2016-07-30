namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TarifsInsteadPaymentSystem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Tarifs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MonthlyPayment = c.Double(nullable: false),
                        AnnualPayment = c.Double(nullable: false),
                        Note = c.String(nullable: false, maxLength: 255),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Children", "Options", c => c.Int(nullable: false));
            AddColumn("dbo.Children", "TarifId", c => c.Int(nullable: false));
            CreateIndex("dbo.Children", "TarifId");
            AddForeignKey("dbo.Children", "TarifId", "dbo.Tarifs", "Id");
            DropColumn("dbo.Children", "IsNobody");
            DropColumn("dbo.Children", "PaymentSystem");
            DropColumn("dbo.PaymentHistory", "PaymentSystem");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PaymentHistory", "PaymentSystem", c => c.Int(nullable: false));
            AddColumn("dbo.Children", "PaymentSystem", c => c.Int(nullable: false));
            AddColumn("dbo.Children", "IsNobody", c => c.Boolean(nullable: false));
            DropForeignKey("dbo.Children", "TarifId", "dbo.Tarifs");
            DropIndex("dbo.Children", new[] { "TarifId" });
            DropColumn("dbo.Children", "TarifId");
            DropColumn("dbo.Children", "Options");
            DropTable("dbo.Tarifs");
        }
    }
}
