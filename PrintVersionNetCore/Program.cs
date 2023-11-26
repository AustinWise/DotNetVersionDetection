using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PrintVersionNetCore
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine(typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            Console.WriteLine(typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
        }
    }
}
