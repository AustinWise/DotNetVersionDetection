using Microsoft.Deployment.DotNet.Releases;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ScrapeNetCoreVersion;

internal class Program
{
    // Versions of .NET Core 3 and expose the product version directly in Environment.Version
    const int MAXIMUM_VERSION = 3;

    // TODO: don't hard code
    const string DOWNLOAD_LOCATION = @"E:\dotnetversions\netcoredownload";
    const string EXTRACT_LOCATION = @"E:\dotnetversions\netcoreruntime";
    const string VERSION_PRINTER = @"D:\src\VersionDetection\Austin.DotNetVersionDetection\PrintVersionNetCore\bin\Debug\";
    const string OUTPUT_FILE = @"D:\src\VersionDetection\Austin.DotNetVersionDetection\Austin.DotNetVersionDetection\DotNetCoreVersion.List.cs";

    static async Task Main(string[] args)
    {
        await Console.Out.WriteLineAsync($"RID: {RuntimeInformation.RuntimeIdentifier}");

        bool isWindows = RuntimeInformation.RuntimeIdentifier.StartsWith("win-");

        string expectedArchiveExtension = isWindows ? ".zip" : ".tar.gz";

        ProductCollection products = await ProductCollection.GetAsync();

        var runtimes = new Dictionary<ReleaseVersion, ReleaseFile>();

        foreach (var product in products)
        {
            if (int.Parse(product.ProductVersion.Split('.')[0]) >= MAXIMUM_VERSION)
                continue;

            IEnumerable<ProductRelease> releases = await product.GetReleasesAsync();

            foreach (var r in releases)
            {
                if (r.Runtime is null || r.Runtime.Version.Prerelease is not null)
                {
                    continue;
                }

                // NOTE: this will probably not work with anything except x64, as support has varied over time
                ReleaseFile? file = r.Runtime.Files.Where(f => f.Rid == RuntimeInformation.RuntimeIdentifier && f.Name.EndsWith(expectedArchiveExtension)).SingleOrDefault();

                if (file is null)
                {
                    if (r.Version.ToString() != "1.0.2" || RuntimeInformation.RuntimeIdentifier == "osx-x64")
                        throw new Exception("Unexpected missing version.");
                    continue;
                }

                if (runtimes.TryGetValue(r.Runtime.Version, out ReleaseFile? existingFile))
                {
                    if (file.Hash != existingFile.Hash)
                    {
                        throw new Exception("Hash mismatch for the same runtime version");
                    }
                }
                else
                {
                    runtimes.Add(r.Runtime.Version, file);
                }
            }
        }

        //await DownloadRuntimes(runtimes);

        //ExtractRuntimes(runtimes, isWindows);

        var versionMap = GetVersionsFromRuntimes(runtimes, isWindows);

        using var fs = File.Create(OUTPUT_FILE);
        using var sw = new StreamWriter(fs);
        sw.WriteLine("using System;");
        sw.WriteLine("using System.Collections.Generic;");
        sw.WriteLine();
        sw.WriteLine("namespace Austin.DotNetVersionDetection");
        sw.WriteLine("{");
        sw.WriteLine("    partial class DotNetCoreVersion");
        sw.WriteLine("    {");
        sw.WriteLine("        static internal Dictionary<Version, DotNetCoreVersion> GetVersionMap()");
        sw.WriteLine("        {");
        sw.WriteLine("            return new Dictionary<Version, DotNetCoreVersion>()");
        sw.WriteLine("            {");
        foreach (var group in versionMap.GroupBy(v => v.SpcVersion))
        {
            if (group.Count() == 1)
            {
                sw.WriteLine($"                {{ Version.Parse(\"{group.Key}\"), new DotNetCoreVersion(Version.Parse(\"{group.Single().RuntimeVersion}\")) }},");
            }
            else
            {
                sw.WriteLine($"                {{");
                sw.WriteLine($"                    Version.Parse(\"{group.Key}\"),");
                sw.WriteLine($"                    new DotNetCoreVersion(new Dictionary<string, Version>()");
                sw.WriteLine($"                    {{");
                foreach (var versionCombo in group)
                {
                    sw.WriteLine($"                        {{ \"{versionCombo.SpcInformationalVersion.Split(' ').Last()}\", Version.Parse(\"{versionCombo.RuntimeVersion}\") }},");
                }
                sw.WriteLine($"                    }})");
                sw.WriteLine($"                }},");
            }
        }
        sw.WriteLine("            };");
        sw.WriteLine("        }");
        sw.WriteLine("    }");
        sw.WriteLine("}");
    }

    private static async Task DownloadRuntimes(Dictionary<ReleaseVersion, ReleaseFile> runtimes)
    {
        var wc = new HttpClient();

        foreach (var kvp in runtimes)
        {
            string destPath = Path.Combine(DOWNLOAD_LOCATION, kvp.Value.FileName);
            if (!File.Exists(destPath) || !await VerifyHashOrDelete(kvp.Value, destPath))
            {
                using (Stream responseStream = await wc.GetStreamAsync(kvp.Value.Address))
                using (Stream fs = File.Create(destPath))
                {
                    await responseStream.CopyToAsync(fs);
                }

                await VerifyHashOrDelete(kvp.Value, destPath, throwOnError: true);
            }
        }
    }

    private static async Task<bool> VerifyHashOrDelete(ReleaseFile file, string destPath, bool throwOnError = false)
    {
        HashAlgorithm hasher;

        if (string.IsNullOrEmpty(file.Hash))
        {
            return true;
        }
        else if (file.Hash.Length == 64)
        {
            hasher = SHA256.Create();
        }
        else if (file.Hash.Length == 128)
        {
            hasher = SHA512.Create();
        }
        else
        {
            throw new Exception("Unexpected hash length");
        }

        byte[] hash;
        using (var fs = File.OpenRead(destPath))
        {
            hash = await hasher.ComputeHashAsync(fs);
        }

        hasher.Dispose();

        string actualHash = string.Join("", hash.Select(h => h.ToString("x2")));

        bool hashOk = actualHash.Equals(file.Hash, StringComparison.OrdinalIgnoreCase);

        if (!hashOk)
        {
            File.Delete(destPath);
            if (throwOnError)
            {
                throw new Exception($"For file {file.FileName}, expected hash {file.Hash} but got {actualHash}");
            }
        }

        return hashOk;
    }

    private static void ExtractRuntimes(Dictionary<ReleaseVersion, ReleaseFile> runtimes, bool isWindows)
    {
        if (!isWindows)
        {
            // TODO: tar.gz
            throw new NotImplementedException();
        }

        foreach (var kvp in runtimes)
        {
            string destPath = Path.Combine(EXTRACT_LOCATION, kvp.Key.ToString());
            var di = Directory.CreateDirectory(destPath);
            if (di.GetFiles().Any())
            {
                // blindly assume we already extracted the runtime
                return;
            }
            string zipPath = Path.Combine(DOWNLOAD_LOCATION, kvp.Value.FileName);
            ZipFile.ExtractToDirectory(zipPath, destPath);
        }
    }

    private static List<VersionCombo> GetVersionsFromRuntimes(Dictionary<ReleaseVersion, ReleaseFile> runtimes, bool isWindows)
    {
        var ret = new List<VersionCombo>();

        string executableExtension = isWindows ? ".exe" : "";

        //foreach (var runtimeVersion in runtimes.Keys)
        Parallel.ForEach(runtimes.Keys, runtimeVersion =>
        {
            if (runtimeVersion.ToString() == "2.0.8")
            {
                // this version hash the exact same commit hash as 2.0.7, at least for win-x64
                return;
            }

            string dotnetExe = Path.Combine(EXTRACT_LOCATION, runtimeVersion.ToString(), "dotnet" + executableExtension);
            string versionPrinter = Path.Combine(VERSION_PRINTER, $"netcoreapp{runtimeVersion.Major}.{runtimeVersion.Minor}", "PrintVersionNetCore.dll");

            var psi = new ProcessStartInfo(dotnetExe, versionPrinter);
            psi.Environment.Add("DOTNET_MULTILEVEL_LOOKUP", "0");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            var p = Process.Start(psi);
            if (p is null)
            {
                throw new Exception("failed to start");
            }
            string[] result = p.StandardOutput.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                throw new Exception("Execution failed");
            }

            var spcVersion = Version.Parse(result[0]);
            string spcInformationalVersion = result[1];
            string frameworkDescription = result[2];

            lock (ret)
                ret.Add(new VersionCombo(spcVersion, spcInformationalVersion, frameworkDescription, new Version(runtimeVersion.Major, runtimeVersion.Minor, runtimeVersion.Patch)));
        });
        return ret;
    }

    record class VersionCombo(Version SpcVersion, string SpcInformationalVersion, string FrameworkDescription, Version RuntimeVersion)
    {
    }
}
