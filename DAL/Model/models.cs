using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Model
{
    public abstract class BaseEntity<TId> : IDataErrorInfo
    {
        [Key]
        public TId Id { get; set; }

        public string this[string columnName]
        {
            get
            {
                string error = GetErrorInternal(columnName);
                Error = error;
                return error;
            }
        }

        protected virtual string GetErrorInternal(string columnName) => null;

        [NotMapped]
        public string Error { get; protected set; }
    }

    public abstract class DateTimeNowAsDefaultEntity : BaseEntity<int>
    {
        private DateTime? _dateTime;
        protected DateTime Date
        {
            get
            {
                if (_dateTime == null)
                {
                    _dateTime = DateTime.Now;
                }
                return _dateTime.Value;
            }
            set
            {
                _dateTime = value;
            }
        }

        /*
         to use:
         
        {
            get { return Date; }
            set { Date = value; }
        }

         */
    }

    public class Group : DateTimeNowAsDefaultEntity
    {
        [Required, MaxLength(64)]
        public string Name { get; set; }

        public Groups GroupType { get; set; }

        public DateTime CreatedDate
        {
            get { return Date; }
            set { Date = value; }
        }

        [MaxLength(256)]
        public string PhotoPath { get; set; }

        public virtual ICollection<Child> Children { get; set; }

        protected override string GetErrorInternal(string columnName)
        {
            string err = null;
            switch (columnName)
            {
                case nameof(Name):
                    if (string.IsNullOrWhiteSpace(Name))
                        err = "Wrong Name";
                    break;
                case nameof(PhotoPath):
                    if (!string.IsNullOrWhiteSpace(PhotoPath) && PhotoPath.Length > 255)
                        return "PhotoPath is too long";
                    break;
            }
            return err;
        }

        public bool IsValid()
        {
            return
                GetErrorInternal(nameof(Name)) == null &&
                GetErrorInternal(nameof(PhotoPath)) == null;
        }
    }

    [Flags]
    public enum Groups : byte
    {
        // pair with all
        Finished = 1,

        // pair with Finished
        Nursery = 2,
        Junior = 4,
        Middle = 6,
        Older = 8,
    }

    public class Person : BaseEntity<int>
    {
        [Required, MaxLength(64)]
        public string FirstName { get; set; }
        [Required, MaxLength(64)]
        public string LastName { get; set; }
        [MaxLength(64)]
        public string Patronymic { get; set; }
        [MaxLength(255)]
        public string PhotoPath { get; set; }

        protected override string GetErrorInternal(string columnName)
        {
            switch (columnName)
            {
                case nameof(FirstName):
                    if (string.IsNullOrWhiteSpace(FirstName))
                        return "Firstname is not valid";
                    break;
                case nameof(LastName):
                    if (string.IsNullOrWhiteSpace(LastName))
                        return "LastName is not valid";
                    break;
                case nameof(Patronymic):
                    if (string.IsNullOrWhiteSpace(Patronymic))
                        return "Patronymic is not valid";
                    break;
                case nameof(PhotoPath):
                    if (!string.IsNullOrWhiteSpace(PhotoPath) && PhotoPath.Length > 255)
                        return "PhotoPath is too long";
                    break;
            }
            return null;
        }

        public bool IsValid()
        {
            return
                GetErrorInternal(nameof(FirstName)) == null &&
                GetErrorInternal(nameof(LastName)) == null &&
                GetErrorInternal(nameof(Patronymic)) == null &&
                GetErrorInternal(nameof(PhotoPath)) == null;
        }

        public override string ToString()
        {
            return $"{nameof(Id)} = {Id} {nameof(FirstName)} = {FirstName}, {nameof(LastName)} = {LastName}, {nameof(Patronymic)} = {Patronymic}.";
        }
    }

    public class Parent : BaseEntity<int>
    {
        public virtual Person Person { get; set; }

        [Required, MaxLength(128)]
        public string LocationAddress { get; set; }
        [MaxLength(128)]
        public string ResidenceAddress { get; set; }
        [MaxLength(128)]
        public string WorkAddress { get; set; }

        [Required, MaxLength(10), MinLength(10)]
        public string PassportSeries { get; set; }
        [Required, MaxLength(256)]
        public string PassportIssuedBy { get; set; }
        public DateTime PassportIssueDate { get; set; }
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        public virtual ICollection<ParentChild> ParentsChildren { get; set; }
    }

    public class Child : DateTimeNowAsDefaultEntity
    {
        public virtual Person Person { get; set; }

        public int GroupId { get; set; }
        public virtual Group Group { get; set; }

        [Required, MaxLength(128)]
        public string LocationAddress { get; set; }

        private DateTime _birthDate;
        public DateTime BirthDate
        {
            get { return _birthDate; }
            set { _birthDate = value.Date; }
        }

        public Sex Sex { get; set; }

        [NotMapped]
        public EnterChild LastEnterChild { get; set; }

        public bool IsNobody { get; set; }

        public int TarifId { get; set; }
        public virtual Tarif Tarif { get; set; }

        public virtual ICollection<ParentChild> ParentsChildren { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }

        public virtual ICollection<EnterChild> EnterChildren { get; set; }

        protected override string GetErrorInternal(string columnName)
        {
            switch (columnName)
            {
                case nameof(LocationAddress):
                    if (string.IsNullOrWhiteSpace(LocationAddress))
                        return "Is null or spaces";
                    break;
                case nameof(Group):
                    if (Group == null && GroupId == 0)
                        return "Group must be chosen";
                    break;
                case nameof(Person):
                    if (Person == null)
                        return "Person cannot be null";
                    break;
            }
            return null;
        }

        public bool IsValid()
        {
            return
                GetErrorInternal(nameof(LocationAddress)) == null &&
                GetErrorInternal(nameof(Group)) == null &&
                Person.IsValid();
        }

        public override string ToString()
        {
            return "Child: " + Person;
        }
    }

    [Table("ParentChild")]
    public class ParentChild
    {
        [Key, Column(Order = 0)]
        public int ChildId { get; set; }
        [Key, Column(Order = 1)]
        public int ParentId { get; set; }

        public virtual Child Child { get; set; }
        public virtual Parent Parent { get; set; }

        public Parents ParentType { get; set; }
    }

    public class Tarif : BaseEntity<int>
    {
        public double MonthlyPayment { get; set; }
        public double AnnualPayment { get; set; }
        [Required, MaxLength(255), MinLength(3)]
        public string Note { get; set; }

        public virtual ICollection<Child> Children { get; set; }

        protected override string GetErrorInternal(string columnName)
        {
            string err = null;
            switch (columnName)
            {
                case nameof(MonthlyPayment):
                    if (MonthlyPayment < 0)
                        err = "Monthly payment cannot be less than zero";
                    break;
                case nameof(AnnualPayment):
                    if (AnnualPayment < 0)
                        err = "Annual payment cannot be less than zero";
                    break;
                case nameof(Note):
                    if (string.IsNullOrWhiteSpace(Note))
                        err = "Note cannot be null or spaces";
                    break;
            }
            return err;
        }

        public bool IsValid()
        {
            return
                GetErrorInternal(nameof(MonthlyPayment)) == null &&
                GetErrorInternal(nameof(AnnualPayment)) == null &&
                GetErrorInternal(nameof(Note)) == null;
        }
    }

    [Table("PaymentHistory")]
    public class Payment : DateTimeNowAsDefaultEntity
    {
        public Payment() { }

        public Payment(double contribution, double paidMoney)
        {
            Debit = contribution - paidMoney;
            PaidMoney = paidMoney;
        }

        public int ChildId { get; set; }
        public virtual Child Child { get; set; }

        public DateTime PaymentDate
        {
            get { return Date; }
            set { Date = value; }
        }

        public double PaidMoney { get; set; }
        public double Debit { get; set; }     // Debit = родитель должен заплатить. -Debit = родителю должны заплатить
    }

    [Table("EnterChildHistory")]
    public class EnterChild : DateTimeNowAsDefaultEntity
    {
        [MaxLength(64)]
        public string ExpulsionNote { get; set; }

        public int ChildId { get; set; }
        public virtual Child Child { get; set; }

        public DateTime EnterDate
        {
            get { return Date; }
            set { Date = value; }
        }

        public DateTime? ExpulsionDate { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(Id)} = {Id}, " +
                $"{nameof(EnterDate)} = {EnterDate}, " +
                $"{nameof(EnterDate)} = {ExpulsionDate?.ToString() ?? "NULL"}";
        }
    }

    public enum Sex : byte { Male = 0, Female = 1 }

    public enum Parents : byte { Father = 0, Mother = 1, Other = 2 }
}
