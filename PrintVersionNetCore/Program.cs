using Austin.DotNetVersionDetection;
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
            Console.WriteLine(DotNetVersion.Detect());
            PropertyInfo prop = typeof(Environment).GetTypeInfo().GetDeclaredProperty("Version");
            Console.WriteLine(prop?.GetValue(null) ?? "null");
        }
    }
}
