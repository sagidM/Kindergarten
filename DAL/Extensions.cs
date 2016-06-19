using System;
using System.Collections.Generic;
using DAL.Model;

namespace DAL
{
    public static class Extensions
    {
        public static bool Add(this ICollection<Child> children, Child child)
        {
            throw new NotImplementedException();
        }
        public static bool AddRange(this ICollection<Child> children, ICollection<Child> additionChildren)
        {
            throw new NotImplementedException();
        }

        internal static T NotNull<T>(this T self)
        {
            if (self == null)
            {
                throw new ArgumentNullException();
            }
            return self;
        }
        internal static T NotNull<T>(this T self, string message)
        {
            if (self == null)
            {
                throw new ArgumentNullException(message);
            }
            return self;
        }
        internal static T NotNull<T>(this T self, string message, string paramName)
        {
            if (self == null)
            {
                throw new ArgumentNullException(paramName, message);
            }
            return self;
        }
    }
}
