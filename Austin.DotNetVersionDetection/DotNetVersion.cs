using System;

namespace Austin.DotNetVersionDetection
{
    public sealed partial class DotNetVersion
    {
        private DotNetVersion(DotNetFlavor flavor, Version version)
        {
            this.Flavor = flavor;
            this.Version = version;
        }

        public DotNetFlavor Flavor { get; }
        public Version Version { get; }

        public override string ToString()
        {
            if (Flavor == DotNetFlavor.Unknown)
            {
                return Flavor.ToString();
            }
            else
            {
                return $"{Flavor} {Version}";
            }
        }
    }
}
