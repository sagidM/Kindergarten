namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeParent : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.Payments", newName: "PaymentHistory");
            AlterColumn("dbo.Parents", "PassportSeries", c => c.String(nullable: false, maxLength: 10));
            AlterColumn("dbo.Parents", "PassportIssuedBy", c => c.String(nullable: false, maxLength: 256));
            AlterColumn("dbo.Parents", "PhoneNumber", c => c.String(maxLength: 20));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Parents", "PhoneNumber", c => c.String());
            AlterColumn("dbo.Parents", "PassportIssuedBy", c => c.String(maxLength: 256));
            AlterColumn("dbo.Parents", "PassportSeries", c => c.String(maxLength: 10));
            RenameTable(name: "dbo.PaymentHistory", newName: "Payments");
        }
    }
}
