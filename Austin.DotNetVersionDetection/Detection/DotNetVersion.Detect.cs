using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Austin.DotNetVersionDetection
{
    public partial class DotNetVersion
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
            const string MONO_PREFIX = "Mono ";

            DotNetVersion ret;
            string frameworkDescription = FrameworkDescription;

            if (frameworkDescription is null)
            {
                // TODO: handle older .NET framework (and mono?)
                throw new NotImplementedException();
            }
            else if (frameworkDescription.StartsWith(DOT_NET_PREFIX))
            {
                if (frameworkDescription.Length > DOT_NET_PREFIX.Length && char.IsNumber(frameworkDescription[DOT_NET_PREFIX.Length]))
                {
                    ret = new DotNetVersion(DotNetFlavor.NetCore, EnvironmentVersion);
                }
                else if (frameworkDescription.StartsWith(".NET Framework"))
                {
                    // TODO
                    // https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
                    throw new NotImplementedException();
                }
                else if (frameworkDescription.StartsWith(".NET Core"))
                {
                    if (EnvironmentVersion.Major == 3)
                    {
                        ret = new DotNetVersion(DotNetFlavor.NetCore, EnvironmentVersion);
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
            else if (frameworkDescription.StartsWith(MONO_PREFIX))
            {
                ret = new DotNetVersion(DotNetFlavor.Mono, Version.Parse(frameworkDescription.Split(' ')[1]));
            }
            else
            {
                ret = new DotNetVersion(DotNetFlavor.Unknown, null);
            }

            return ret;
        }

        private static string FrameworkDescription
        {
            get
            {
#if NET45
                // Even though we are targeting a .NET Framework that does not have this type, we could
                // be running on a .NET Framework that does have it. Or we could even hypothetically be running on a .NET Core
                // if someone messed up their dependencies. So this might exist
                Type t = Type.GetType("System.Runtime.InteropServices.RuntimeInformation, System.Runtime.InteropServices.RuntimeInformation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                if (t is null)
                {
                    return null;
                }
                PropertyInfo prop = t.GetProperty("FrameworkDescription", BindingFlags.Static);
                return (string)prop?.GetValue(null);
#else
                return RuntimeInformation.FrameworkDescription;
#endif
            }
        }

        private static Version EnvironmentVersion
        {
            get
            {
#if NETSTANDARD1_1
                // All versions of .NET Framework and .NET Core have this property
                PropertyInfo prop = typeof(Environment).GetTypeInfo().GetDeclaredProperty("Version");
                return (Version)prop.GetValue(null);
#else
                return Environment.Version;
#endif
            }
        }
    }
}
