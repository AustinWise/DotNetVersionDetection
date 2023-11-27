using Microsoft.Deployment.DotNet.Releases;
using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ScrapeNetCoreVersion;

internal class Program
{
    // Versions of .NET Core 3 and expose the product version directly in Environment.Version
    const int MAXIMUM_VERSION = 3;

    readonly bool _offline;
    readonly string _rid;
    readonly string _downloadLocation;
    readonly string _extractLocation;
    readonly string _versionPrinterPath;

    static async Task Main(string[] args)
    {
        var dotnetStoragePathOption = new Option<string>(
            name: "--dotnets-path",
            description: "Where to store dotnet downloads and extracted runtimes. Also settable with the AUSTIN_DOTNETS_PATH environmental variable",
            getDefaultValue: () => Environment.GetEnvironmentVariable("AUSTIN_DOTNETS_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dotnets"));

        var ridOption = new Option<string>(
            name: "--rid",
            description: "The runtime identifier of dotnet to use.",
            getDefaultValue: () => RuntimeInformation.RuntimeIdentifier
        );

        var offlineOption = new Option<bool>(
            name: "--offline",
            description: "Don't download any information from the internet, just use previously downloaded dotnets."
        );

        var rootCommand = new RootCommand("For getting version information and testing.")
        {
            dotnetStoragePathOption,
            ridOption,
            offlineOption,
        };

        var scrapeCommand = new Command("scrape", "Scrape version information from .NET Core versions.")
        {
        };
        rootCommand.Add(scrapeCommand);

        scrapeCommand.SetHandler(async (rid, dotnetStoragePath, offline) =>
        {
            var prog = new Program(offline, rid, dotnetStoragePath);
            await prog.DownloadAndExtractIfNotOffline();
            var versionMap = await prog.GetVersionsFromRuntimes();
            WriteDotnetCoreInfo(versionMap);
        }, ridOption, dotnetStoragePathOption, offlineOption);

        await rootCommand.InvokeAsync(args);
    }

    private async Task DownloadAndExtractIfNotOffline()
    {
        if (!_offline)
        {
            ProductCollection products = await ProductCollection.GetAsync();
            var runtimes = await GetProductInfo(products);
            await DownloadRuntimes(runtimes);
            await ExtractRuntimes(runtimes);
        }
    }

    bool IsWindows => _rid.StartsWith("win-");
    string ExpectedArchiveExtension => IsWindows ? ".zip" : ".tar.gz";

    private Program(bool offline, string rid, string dotnetsRoot)
    {
        this._offline = offline;
        this._rid = rid;
        this._downloadLocation = Path.Combine(dotnetsRoot, rid, "downloads");
        this._extractLocation = Path.Combine(dotnetsRoot, rid, "extracted");
        this._versionPrinterPath = Path.Combine(Environment.CurrentDirectory, "..", "PrintVersionNetCore", "bin", "debug");
        Directory.CreateDirectory(_downloadLocation);
        Directory.CreateDirectory(_extractLocation);
    }

    private static void WriteDotnetCoreInfo(List<VersionCombo> versionMap)
    {
        using var fs = File.Create(Path.Combine(Environment.CurrentDirectory, "..", "Austin.DotNetVersionDetection", "Detection", "DotNetCoreVersion.List.cs"));
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
        foreach (var group in versionMap.GroupBy(v => v.SpcVersion).OrderBy(v => v.Key))
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
                foreach (var versionCombo in group.OrderBy(v => v.RuntimeVersion))
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

    private async Task<Dictionary<ReleaseVersion, ReleaseFile>> GetProductInfo(ProductCollection products)
    {
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

                if (r.Version.ToString() == "2.0.8")
                {
                    // 2.0.8 only rebuilt ASP.NET, not the runtime itself.
                    continue;
                }

                // NOTE: this will probably not work with anything except x64, as support has varied over time
                ReleaseFile? file = r.Runtime.Files.Where(f => f.Rid == _rid && f.FileName.EndsWith(ExpectedArchiveExtension)).SingleOrDefault();

                if (file is null)
                {
                    // oddly, 1.0.2 only has an OSX .pkg file and nothing else.
                    if (r.Version.ToString() != "1.0.2")
                    {
                        string foundFiles = string.Join(", ", r.Runtime.Files.Select(f => f.FileName));
                        throw new Exception($"Could not find any files for version {r.Version}. Found these files: {foundFiles}");
                    }
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

        return runtimes;
    }

    private async Task DownloadRuntimes(Dictionary<ReleaseVersion, ReleaseFile> runtimes)
    {
        using var wc = new HttpClient();

        await Parallel.ForEachAsync(runtimes, async (kvp, ct) =>
        {
            string destPath = Path.Combine(_downloadLocation, kvp.Value.FileName);
            if (!File.Exists(destPath) || !await VerifyHashOrDelete(kvp.Value, destPath))
            {
                Console.WriteLine("Downloading " + kvp.Key.ToString());

                using (Stream responseStream = await wc.GetStreamAsync(kvp.Value.Address))
                using (Stream fs = File.Create(destPath))
                {
                    await responseStream.CopyToAsync(fs);
                }

                await VerifyHashOrDelete(kvp.Value, destPath, throwOnError: true);

                Console.WriteLine("Downloaded " + kvp.Key.ToString());
            }
        });
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

    private async Task ExtractRuntimes(Dictionary<ReleaseVersion, ReleaseFile> runtimes)
    {
        await Parallel.ForEachAsync(runtimes, async (kvp, ct) =>
        {
            string destPath = Path.Combine(_extractLocation, kvp.Key.ToString());
            var di = Directory.CreateDirectory(destPath);
            if (di.GetFiles().Any())
            {
                // blindly assume we already extracted the runtime
                return;
            }

            Console.WriteLine($"Extracting {kvp.Key}");

            string archivePath = Path.Combine(_downloadLocation, kvp.Value.FileName);
            try
            {
                if (IsWindows)
                {
                    ZipFile.ExtractToDirectory(archivePath, destPath);
                }
                else
                {
                    await using var fs = File.OpenRead(archivePath);
                    await using var gz = new GZipStream(fs, CompressionMode.Decompress);
                    await System.Formats.Tar.TarFile.ExtractToDirectoryAsync(gz, destPath, false, ct);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to extract " + archivePath, ex);
            }

            Console.WriteLine($"Extracted {kvp.Key}");
        });
    }

    private async Task<List<VersionCombo>> GetVersionsFromRuntimes()
    {
        var ret = new List<VersionCombo>();

        string executableExtension = IsWindows ? ".exe" : "";

        await Parallel.ForEachAsync(new DirectoryInfo(_extractLocation).GetDirectories(), async (dotnetFolder, ct) =>
        {
            var runtimeVersion = Version.Parse(dotnetFolder.Name);
            string dotnetExe = Path.Combine(dotnetFolder.FullName, "dotnet" + executableExtension);
            string versionPrinter = Path.Combine(_versionPrinterPath, $"netcoreapp{runtimeVersion.Major}.{runtimeVersion.Minor}", "PrintVersionNetCore.dll");

            var psi = new ProcessStartInfo(dotnetExe, versionPrinter);
            psi.Environment.Add("DOTNET_MULTILEVEL_LOOKUP", "0");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            var p = Process.Start(psi);
            if (p is null)
            {
                throw new Exception("failed to start");
            }
            string allOutput = await p.StandardOutput.ReadToEndAsync();
            string[] result = allOutput.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            await p.WaitForExitAsync();

            if (p.ExitCode != 0)
            {
                throw new Exception("Execution failed");
            }

            var spcVersion = Version.Parse(result[0]);
            string spcInformationalVersion = result[1];
            string frameworkDescription = result[2];

            lock (ret)
                ret.Add(new VersionCombo(spcVersion, spcInformationalVersion, frameworkDescription, runtimeVersion));
        });
        return ret;
    }

    record class VersionCombo(Version SpcVersion, string SpcInformationalVersion, string FrameworkDescription, Version RuntimeVersion)
    {
    }
}
