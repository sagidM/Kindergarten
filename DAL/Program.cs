using System;
using System.Linq;
using DAL.Model;

namespace DAL
{
    internal class Program
    {
        static void Main()
        {
//            EFlogger.EntityFramework6.EFloggerFor6.Initialize();

            var k = new KindergartenContext();

            var group = RemoveFirstParentChildPairWithPeople(k);
            k.Groups.Remove(group);

            k.SaveChanges();
        }

        private static Group RemoveFirstParentChildPairWithPeople(KindergartenContext k)
        {
            var pair = k.ParentChildren
                .Include("Child.Group")
                .Include("Parent")
                .Include("Child.Person")
                .Include("Parent.Person")
                .First();
            var group = pair.Child.Group;
            var cperson = pair.Child.Person;
            var pperson = pair.Parent.Person;
            k.People.Remove(cperson);
            k.People.Remove(pperson);

            return group;
        }

        // ReSharper disable once UnusedMember.Local
        private static ParentChild AddParentChild(KindergartenContext k)
        {
            var group = new Group
            {
                Name = "Солнышко",
                GroupType = Groups.Nursery,
                PhotoPath = "sun_photo",
            };
//            k.Groups.Add(group);
            var child = new Child
            {
                Person = new Person
                {
                    FirstName = "Иван",
                    LastName = "Иванов",
                    Patronymic = "Иванович",
                },
                BirthDate = new DateTime(2014, 1, 4),
                Group = @group,
                LocationAddress = "ул. Ленина д. 12",
                Sex = Sex.Male,
            };
//            k.Children.Add(child);
            var parent = new Parent
            {
                LocationAddress = "ул. Ленина д. 12",
                ResidenceAddress = "ул. Ленина д. 12",
                WorkAddress = "ул. Ленина д. 73",
                PassportIssueDate = new DateTime(1990, 04, 12),
                PassportIssuedBy = "ОУФМС России по р. ХХХ",
                PassportSeries = "0123456789",
                PhoneNumber = "+7 912 345 67 89",
                Person = new Person
                {
                    FirstName = "Сергей",
                    LastName = "Иванов",
                    Patronymic = "Васильевич",
                },
            };
//            k.Parents.Add(parent);

            var parentChild = new ParentChild
            {
                Child = child,
                Parent = parent,
                ParentType = Parents.Father,
            };
            k.ParentChildren
                .Add(parentChild);
            return parentChild;
        }
    }
}
