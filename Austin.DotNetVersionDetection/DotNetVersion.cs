using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Austin.DotNetVersionDetection
{
    public class DotNetVersion
    {
        private volatile static DotNetVersion s_version = null;

        public static DotNetVersion Detect()
        {
            DotNetVersion ret = s_version;
            if (ret == null)
            {
                ret = DetectCore();

                DotNetVersion prevVersion = Interlocked.CompareExchange(ref s_version, ret, null);
                if (prevVersion != null)
                {
                    ret = prevVersion;
                }
            }
            return ret;
        }

        private static DotNetVersion DetectCore()
        {
            const string DOT_NET_PREFIX = ".NET ";

            DotNetVersion ret;
            string frameworkDescription = RuntimeInformation.FrameworkDescription;
            if (frameworkDescription.StartsWith(DOT_NET_PREFIX))
            {
                if (frameworkDescription.Length > DOT_NET_PREFIX.Length && char.IsNumber(frameworkDescription[DOT_NET_PREFIX.Length]))
                {
                    ret = new DotNetVersion(DotNetFlavor.NetCore, Environment.Version);
                }
                else if (frameworkDescription.StartsWith(".NET Framework"))
                {
                    // TODO
                    // https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
                    throw new NotImplementedException();
                }
                else if (frameworkDescription.StartsWith(".NET Core"))
                {
                    if (Environment.Version.Major == 3)
                    {
                        ret = new DotNetVersion(DotNetFlavor.NetCore, Environment.Version);
                    }
                    else
                    {
                        var spcVersion = typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                        DotNetCoreVersion dncVer;
                        if (!DotNetCoreVersion.GetVersionMap().TryGetValue(Version.Parse(spcVersion), out dncVer))
                        {
                            throw new Exception("Could not find SPC version");
                        }
                        if (dncVer.DotNetVersion != null)
                        {
                            ret = new DotNetVersion(DotNetFlavor.NetCore, dncVer.DotNetVersion);
                        }
                        else
                        {
                            var infoVersion = typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                            string[] infoVersionSplits = infoVersion.Split(' ');
                            if (dncVer.CommitToDonetVersion.TryGetValue(infoVersionSplits[infoVersionSplits.Length - 1], out Version netCoreVersion))
                            {
                                ret = new DotNetVersion(DotNetFlavor.NetCore, netCoreVersion);
                            }
                            else
                            {
                                throw new Exception("Could not find version by commit hash");
                            }
                        }
                    }
                }
                else if (frameworkDescription.StartsWith(".NET Native"))
                {
                    // TODO
                    throw new NotImplementedException();
                }
                else
                {
                    ret = new DotNetVersion(DotNetFlavor.Unknown, null);
                }
            }
            else
            {
                ret = new DotNetVersion(DotNetFlavor.Unknown, null);
            }

            return ret;
        }

        private DotNetVersion(DotNetFlavor flavor, Version version)
        {
            this.Flavor = flavor;
            this.Version = version;
        }

        public DotNetFlavor Flavor { get; }
        public Version Version { get; }
    }
}
