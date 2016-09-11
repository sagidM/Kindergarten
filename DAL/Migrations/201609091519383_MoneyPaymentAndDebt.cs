namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MoneyPaymentAndDebt : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.RangePayments", "HadToPayMoney", "DebtAfterPaying");
            RenameColumn("dbo.MonthlyPayments", "HadToPayMoney", "DebtAfterPaying");
            AddColumn("dbo.RangePayments", "MoneyPaymentByTarif", c => c.Double(nullable: false));
            AddColumn("dbo.MonthlyPayments", "MoneyPaymentByTarif", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.MonthlyPayments", "MoneyPaymentByTarif");
            DropColumn("dbo.RangePayments", "MoneyPaymentByTarif");
            RenameColumn("dbo.MonthlyPayments", "DebtAfterPaying", "HadToPayMoney");
            RenameColumn("dbo.RangePayments", "DebtAfterPaying", "HadToPayMoney");
        }
    }
}
