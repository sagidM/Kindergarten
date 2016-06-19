using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Model;
using DAL.Properties;

namespace DAL
{
    class Program
    {
        static void Main(string[] args)
        {
            var obj = new KindergartenContext()
                .Children
                .ToList();

            Console.WriteLine(obj);
        }
    }
}
