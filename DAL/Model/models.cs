using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Model
{
    public abstract class BaseEntity<TId>
    {
        [Key]
        public TId Id { get; set; }
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
    }
    public enum Groups
    {
        Nursery,
        Junior,
        Middle,
        Older,
        Finished,
    }

    public class Person : BaseEntity<int>
    {
        [Required, MaxLength(64)]
        public string FirstName { get; set; }
        [Required, MaxLength(64)]
        public string LastName { get; set; }
        [MaxLength(64)]
        public string Patronymic { get; set; }
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

        [Required, MaxLength(10)]
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

        #region Group

        public int GroupId { get; set; }
        public Group Group { get; set; }

        #endregion

        [Required, MaxLength(128)]
        public string LocationAddress { get; set; }

        public DateTime BirthDate { get; set; }
        public Sex Sex { get; set; }

        public DateTime EnterDate
        {
            get { return Date; }
            set { Date = value; }
        }
        
        public bool IsNobody { get; set; }

        public PaymentSystems PaymentSystem { get; set; }

        public virtual ICollection<ParentChild> ParentsChildren { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
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
        public Child Child { get; set; }

        public DateTime PaymentDate
        {
            get { return Date; }
            set { Date = value; }
        }

        public PaymentSystems PaymentSystem { get; set; }

        public double PaidMoney { get; set; }
        public double Debit { get; set; }     // Debit = родитель должен заплатить. -Debit = родителю должны заплатить
    }


    public enum PaymentSystems
    {
        System1,
        System2,
    }
    public enum Sex : byte { Male, Female }

    public enum Parents : byte { Father, Mother, Other }
}
