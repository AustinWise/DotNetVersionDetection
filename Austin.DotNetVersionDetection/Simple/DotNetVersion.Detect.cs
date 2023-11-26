using System;

namespace Austin.DotNetVersionDetection
{
    public partial class DotNetVersion
    {
        // We make the assumption that is is not possible to run a .NET Core 3 assembly on ealier versions of .NET
        // or the .NET Framework.
        private static readonly DotNetVersion s_version = new DotNetVersion(DotNetFlavor.NetCore, Environment.Version);

        public static DotNetVersion Detect()
        {
            return s_version;
        }
    }
}
