using Microsoft.Win32;
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
                ret = DetectDotnetFrameworkOrMono();
            }
            else if (frameworkDescription.StartsWith(DOT_NET_PREFIX))
            {
                if (frameworkDescription.Length > DOT_NET_PREFIX.Length && char.IsNumber(frameworkDescription[DOT_NET_PREFIX.Length]))
                {
                    ret = new DotNetVersion(DotNetFlavor.NetCore, EnvironmentVersion);
                }
                else if (frameworkDescription.StartsWith(".NET Framework"))
                {
                    ret = DetectDotnetFrameworkOrMono();
                }
                else if (frameworkDescription.StartsWith(".NET Core"))
                {
                    ret = DetectNetCoreVersion();
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

        private static DotNetVersion DetectNetCoreVersion()
        {
            if (EnvironmentVersion.Major == 3)
            {
                return new DotNetVersion(DotNetFlavor.NetCore, EnvironmentVersion);
            }

            var spcVersion = typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            DotNetCoreVersion dncVer;
            if (!DotNetCoreVersion.GetVersionMap().TryGetValue(Version.Parse(spcVersion), out dncVer))
            {
                throw new Exception("Could not find SPC version");
            }
            if (dncVer.DotNetVersion != null)
            {
                return new DotNetVersion(DotNetFlavor.NetCore, dncVer.DotNetVersion);
            }
            else
            {
                var infoVersion = typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                string[] infoVersionSplits = infoVersion.Split(' ');
                if (dncVer.CommitToDonetVersion.TryGetValue(infoVersionSplits[infoVersionSplits.Length - 1], out Version netCoreVersion))
                {
                    return new DotNetVersion(DotNetFlavor.NetCore, netCoreVersion);
                }
                else
                {
                    throw new Exception("Could not find version by commit hash");
                }
            }
        }

        private static DotNetVersion DetectDotnetFrameworkOrMono()
        {
            TypeInfo monoType = typeof(object).GetTypeInfo().Assembly.GetType("Mono.Runtime")?.GetTypeInfo();
            if (monoType != null)
            {
                MethodInfo displayNameMethod = monoType.GetDeclaredMethod("GetDisplayName");
                if (displayNameMethod == null)
                {
                    string displayName = (string)displayNameMethod.Invoke(null, null);
                    return new DotNetVersion(DotNetFlavor.Mono, Version.Parse(displayName.Split(' ')[0]));
                }
                else
                {
                    return new DotNetVersion(DotNetFlavor.Mono, null);
                }
            }

            // RegistryView.Registry64 maps to KEY_WOW64_64KEY and is ignored by 32-bit versions of Windows
            // https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-key-security-and-access-rights
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var subkey = key.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
            if (subkey == null)
            {
                // this should not happen for our current target frameworks.
                throw new Exception("Missing registry key for .NET installation.");
            }

            int release = (int)subkey.GetValue("Release");

            Version ret;
            if (release >= 533320)
                ret = new Version(4, 8, 1);
            else if (release >= 528040)
                ret = new Version(4, 8);
            else if (release >= 461808)
                ret = new Version(4, 7, 2);
            else if (release >= 461308)
                ret = new Version(4, 7, 1);
            else if (release >= 460798)
                ret = new Version(4, 7);
            else if (release >= 394802)
                ret = new Version(4, 6, 2);
            else if (release >= 394254)
                ret = new Version(4, 6, 1);
            else if (release >= 393295)
                ret = new Version(4, 6);
            else if (release >= 379893)
                ret = new Version(4, 5, 2);
            else if (release >= 378675)
                ret = new Version(4, 5, 1);
            else if (release >= 378389)
                ret = new Version(4, 5);
            else
                throw new Exception("Unexpected release: " + release);

            return new DotNetVersion(DotNetFlavor.NetFramework, ret);
        }

        private static string FrameworkDescription
        {
            get
            {
#if NET45
                // Even though we are targeting a .NET Framework that does not have this type, we could
                // be running on a .NET Framework that does have it. Or we could even hypothetically be running on a .NET Core
                // if someone messed up their dependencies. So this might exist.
                Type t = Type.GetType("System.Runtime.InteropServices.RuntimeInformation, System.Runtime.InteropServices.RuntimeInformation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                if (t is null)
                {
                    return null;
                }
                PropertyInfo prop = t.GetProperty("FrameworkDescription");
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
#if NETSTANDARD1_3
                // All versions of .NET Framework and .NET Core have this property
                PropertyInfo prop = typeof(Environment).GetTypeInfo().GetDeclaredProperty("Version");
                return (Version)prop?.GetValue(null) ?? new Version(4, 0);
#else
                return Environment.Version;
#endif
            }
        }
    }
}
