namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePaidMoneyAndDebtFromAnnualPayment : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.RangePayments", "PaidMoney");
            DropColumn("dbo.RangePayments", "DebtAfterPaying");
        }
        
        public override void Down()
        {
            AddColumn("dbo.RangePayments", "DebtAfterPaying", c => c.Double(nullable: false));
            AddColumn("dbo.RangePayments", "PaidMoney", c => c.Double(nullable: false));
        }
    }
}
