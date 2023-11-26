using System;
using System.Collections.Generic;

namespace Austin.DotNetVersionDetection
{
    internal partial class DotNetCoreVersion
    {
        public Version DotNetVersion { get; }
        public Dictionary<string, Version> CommitToDonetVersion { get; set; }

        public DotNetCoreVersion(Version dotNetVersion)
        {
            this.DotNetVersion = dotNetVersion;
        }

        public DotNetCoreVersion(Dictionary<string, Version> commitToDonetVersion)
        {
            this.CommitToDonetVersion = commitToDonetVersion;
        }
    }
}
