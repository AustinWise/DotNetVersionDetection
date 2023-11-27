using System;
using System.Collections.Generic;

namespace Austin.DotNetVersionDetection
{
    partial class DotNetCoreVersion
    {
        static internal Dictionary<Version, DotNetCoreVersion> GetVersionMap()
        {
            return new Dictionary<Version, DotNetCoreVersion>()
            {
                { Version.Parse("4.6.1.0"), new DotNetCoreVersion(Version.Parse("2.0.0")) },
                { Version.Parse("4.6.24214.1"), new DotNetCoreVersion(Version.Parse("1.0.0")) },
                { Version.Parse("4.6.24410.1"), new DotNetCoreVersion(Version.Parse("1.0.1")) },
                { Version.Parse("4.6.24628.1"), new DotNetCoreVersion(Version.Parse("1.1.0")) },
                { Version.Parse("4.6.24709.1"), new DotNetCoreVersion(Version.Parse("1.0.3")) },
                { Version.Parse("4.6.25009.1"), new DotNetCoreVersion(Version.Parse("1.0.4")) },
                { Version.Parse("4.6.25009.3"), new DotNetCoreVersion(Version.Parse("1.1.1")) },
                { Version.Parse("4.6.25211.1"), new DotNetCoreVersion(Version.Parse("1.1.2")) },
                { Version.Parse("4.6.25211.2"), new DotNetCoreVersion(Version.Parse("1.0.5")) },
                { Version.Parse("4.6.25714.2"), new DotNetCoreVersion(Version.Parse("1.0.7")) },
                { Version.Parse("4.6.25714.3"), new DotNetCoreVersion(Version.Parse("1.1.4")) },
                { Version.Parse("4.6.25814.5"), new DotNetCoreVersion(Version.Parse("1.0.8")) },
                { Version.Parse("4.6.25815.2"), new DotNetCoreVersion(Version.Parse("2.0.3")) },
                { Version.Parse("4.6.25815.4"), new DotNetCoreVersion(Version.Parse("1.1.5")) },
                { Version.Parse("4.6.25921.1"), new DotNetCoreVersion(Version.Parse("2.0.4")) },
                {
                    Version.Parse("4.6.26011.1"),
                    new DotNetCoreVersion(new Dictionary<string, Version>()
                    {
                        { "f2153aa3636eae670b1a5e7b1d6ba9e72fb0e119", Version.Parse("1.0.9") },
                        { "43a8539a2b3c38d27dc58c6d12034a3b7df5db44", Version.Parse("1.1.6") },
                    })
                },
                { Version.Parse("4.6.26020.3"), new DotNetCoreVersion(Version.Parse("2.0.5")) },
                {
                    Version.Parse("4.6.26201.1"),
                    new DotNetCoreVersion(new Dictionary<string, Version>()
                    {
                        { "400bffd32fb9584f4800f788407dbf4e52f0ae90", Version.Parse("1.0.10") },
                        { "8b685c219f2c83fb32bc2105d67ee39975cd2096", Version.Parse("1.1.7") },
                    })
                },
                { Version.Parse("4.6.26212.1"), new DotNetCoreVersion(Version.Parse("2.0.6")) },
                {
                    Version.Parse("4.6.26328.1"),
                    new DotNetCoreVersion(new Dictionary<string, Version>()
                    {
                        { "fcfe15acadb15545b0a51683d283803445d8bbb9", Version.Parse("1.0.11") },
                        { "4fc99b8b5e0ac61b661359d8e633001137ae1a5c", Version.Parse("1.1.8") },
                        { "b8c69ed222a1e6e5392783cbb4df5faa87be349e", Version.Parse("2.0.7") },
                        { "b8c69ed222a1e6e5392783cbb4df5faa87be349e", Version.Parse("2.0.8") },
                    })
                },
                { Version.Parse("4.6.26515.7"), new DotNetCoreVersion(Version.Parse("2.1.0")) },
                { Version.Parse("4.6.26606.2"), new DotNetCoreVersion(Version.Parse("2.1.1")) },
                { Version.Parse("4.6.26614.1"), new DotNetCoreVersion(Version.Parse("2.0.9")) },
                { Version.Parse("4.6.26623.1"), new DotNetCoreVersion(Version.Parse("1.0.12")) },
                { Version.Parse("4.6.26625.1"), new DotNetCoreVersion(Version.Parse("1.1.9")) },
                { Version.Parse("4.6.26628.5"), new DotNetCoreVersion(Version.Parse("2.1.2")) },
                { Version.Parse("4.6.26725.6"), new DotNetCoreVersion(Version.Parse("2.1.3")) },
                { Version.Parse("4.6.26814.3"), new DotNetCoreVersion(Version.Parse("2.1.4")) },
                {
                    Version.Parse("4.6.26906.1"),
                    new DotNetCoreVersion(new Dictionary<string, Version>()
                    {
                        { "21f78e28bdc53dba2e9977e804ed2f772796d693", Version.Parse("1.0.13") },
                        { "0f281b7f7391054d96e93b96ece458404d29d786", Version.Parse("1.1.10") },
                    })
                },
                { Version.Parse("4.6.26919.2"), new DotNetCoreVersion(Version.Parse("2.1.5")) },
                { Version.Parse("4.6.27019.6"), new DotNetCoreVersion(Version.Parse("2.1.6")) },
                { Version.Parse("4.6.27110.4"), new DotNetCoreVersion(Version.Parse("2.2.0")) },
                { Version.Parse("4.6.27129.4"), new DotNetCoreVersion(Version.Parse("2.1.7")) },
                { Version.Parse("4.6.27207.3"), new DotNetCoreVersion(Version.Parse("2.2.1")) },
                {
                    Version.Parse("4.6.27316.1"),
                    new DotNetCoreVersion(new Dictionary<string, Version>()
                    {
                        { "222046740dd87d4f03951a8efd1aed5d70f9efe0", Version.Parse("1.0.14") },
                        { "e94b2e9113aae9799b7e1a6c6cd2a945d7a6b4cd", Version.Parse("1.1.11") },
                    })
                },
                { Version.Parse("4.6.27317.3"), new DotNetCoreVersion(Version.Parse("2.1.8")) },
                { Version.Parse("4.6.27317.7"), new DotNetCoreVersion(Version.Parse("2.2.2")) },
                { Version.Parse("4.6.27414.5"), new DotNetCoreVersion(Version.Parse("2.2.3")) },
                { Version.Parse("4.6.27414.6"), new DotNetCoreVersion(Version.Parse("2.1.9")) },
                { Version.Parse("4.6.27415.2"), new DotNetCoreVersion(Version.Parse("1.1.12")) },
                { Version.Parse("4.6.27415.3"), new DotNetCoreVersion(Version.Parse("1.0.15")) },
                { Version.Parse("4.6.27514.2"), new DotNetCoreVersion(Version.Parse("2.1.10")) },
                { Version.Parse("4.6.27521.2"), new DotNetCoreVersion(Version.Parse("2.2.4")) },
                { Version.Parse("4.6.27617.4"), new DotNetCoreVersion(Version.Parse("2.1.11")) },
                { Version.Parse("4.6.27617.5"), new DotNetCoreVersion(Version.Parse("2.2.5")) },
                {
                    Version.Parse("4.6.27618.2"),
                    new DotNetCoreVersion(new Dictionary<string, Version>()
                    {
                        { "40705020f56b7634f247e355b1745df362ecd9a7", Version.Parse("1.0.16") },
                        { "144c15ead4c88a8c3e8840cb8b8b0a87e03019f8", Version.Parse("1.1.13") },
                    })
                },
                { Version.Parse("4.6.27817.1"), new DotNetCoreVersion(Version.Parse("2.1.12")) },
                { Version.Parse("4.6.27817.3"), new DotNetCoreVersion(Version.Parse("2.2.6")) },
                { Version.Parse("4.6.28008.1"), new DotNetCoreVersion(Version.Parse("2.1.13")) },
                { Version.Parse("4.6.28008.2"), new DotNetCoreVersion(Version.Parse("2.2.7")) },
                { Version.Parse("4.6.28207.3"), new DotNetCoreVersion(Version.Parse("2.2.8")) },
                { Version.Parse("4.6.28207.4"), new DotNetCoreVersion(Version.Parse("2.1.14")) },
                { Version.Parse("4.6.28325.1"), new DotNetCoreVersion(Version.Parse("2.1.15")) },
                { Version.Parse("4.6.28516.3"), new DotNetCoreVersion(Version.Parse("2.1.16")) },
                { Version.Parse("4.6.28619.1"), new DotNetCoreVersion(Version.Parse("2.1.17")) },
                { Version.Parse("4.6.28801.4"), new DotNetCoreVersion(Version.Parse("2.1.18")) },
                { Version.Parse("4.6.28928.1"), new DotNetCoreVersion(Version.Parse("2.1.19")) },
                { Version.Parse("4.6.29017.1"), new DotNetCoreVersion(Version.Parse("2.1.20")) },
                { Version.Parse("4.6.29130.1"), new DotNetCoreVersion(Version.Parse("2.1.21")) },
                { Version.Parse("4.6.29220.3"), new DotNetCoreVersion(Version.Parse("2.1.22")) },
                { Version.Parse("4.6.29321.3"), new DotNetCoreVersion(Version.Parse("2.1.23")) },
                { Version.Parse("4.6.29518.1"), new DotNetCoreVersion(Version.Parse("2.1.24")) },
                { Version.Parse("4.6.29719.3"), new DotNetCoreVersion(Version.Parse("2.1.25")) },
                { Version.Parse("4.6.29812.2"), new DotNetCoreVersion(Version.Parse("2.1.26")) },
                { Version.Parse("4.6.29916.1"), new DotNetCoreVersion(Version.Parse("2.1.27")) },
                { Version.Parse("4.6.30015.1"), new DotNetCoreVersion(Version.Parse("2.1.28")) },
                { Version.Parse("4.6.30411.1"), new DotNetCoreVersion(Version.Parse("2.1.30")) },
            };
        }
    }
}
