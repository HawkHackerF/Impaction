using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class RDDAPI
{
    private const string HostPath = "https://setup-aws.rbxcdn.com";
    private static WebClient _webClient;
    private static string _appDataPath;

    private static readonly Dictionary<string, string> ExtractPaths = new Dictionary<string, string>
    {
        ["RobloxApp.zip"] = "",
        ["redist.zip"] = "",
        ["shaders.zip"] = "shaders",
        ["ssl.zip"] = "ssl",
        ["WebView2.zip"] = "",
        ["WebView2RuntimeInstaller.zip"] = "WebView2RuntimeInstaller",
        ["content-avatar.zip"] = "content/avatar",
        ["content-configs.zip"] = "content/configs",
        ["content-fonts.zip"] = "content/fonts",
        ["content-sky.zip"] = "content/sky",
        ["content-sounds.zip"] = "content/sounds",
        ["content-textures2.zip"] = "content/textures",
        ["content-models.zip"] = "content/models",
        ["content-platform-fonts.zip"] = "PlatformContent/pc/fonts",
        ["content-platform-dictionaries.zip"] = "PlatformContent/pc/shared_compression_dictionaries",
        ["content-terrain.zip"] = "PlatformContent/pc/terrain",
        ["content-textures3.zip"] = "PlatformContent/pc/textures",
        ["extracontent-luapackages.zip"] = "ExtraContent/LuaPackages",
        ["extracontent-translations.zip"] = "ExtraContent/translations",
        ["extracontent-models.zip"] = "ExtraContent/models",
        ["extracontent-textures.zip"] = "ExtraContent/textures",
        ["extracontent-places.zip"] = "ExtraContent/places"
    };

    static RDDAPI()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        _webClient = new WebClient();
        _webClient.Headers.Add("User-Agent", "RDDAPI/1.0");
        _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    public static string GetInstallationPath()
    {
        return Path.Combine(_appDataPath, "MainRoblox");
    }

    public static bool IsRobloxInstalled()
    {
        string mainFolder = GetInstallationPath();
        string exePath = Path.Combine(mainFolder, "RobloxPlayerBeta.exe");
        return Directory.Exists(mainFolder) && File.Exists(exePath);
    }

    public static string GetLatestVersionHash(string channel = "LIVE")
    {
        string endpoint = $"https://clientsettings.roblox.com/v2/client-version/WindowsPlayer/channel/{channel}";
        try
        {
            string json = _webClient.DownloadString(endpoint);
            if (json.Contains("\"clientVersionUpload\":"))
            {
                int startIndex = json.IndexOf("\"clientVersionUpload\":\"") + 23;
                int endIndex = json.IndexOf("\"", startIndex);
                if (startIndex > 22 && endIndex > startIndex)
                {
                    return json.Substring(startIndex, endIndex - startIndex);
                }
            }
            throw new Exception("clientVersionUpload not found");
        }
        catch (WebException ex)
        {
            throw new Exception($"Failed to fetch version: {ex.Message}");
        }
    }

    public static async Task<bool> Download(string channel = "LIVE", Action<string> progressCallback = null)
    {
        return await Task.Run(() => DownloadSync(channel, progressCallback));
    }

    private static bool DownloadSync(string channel, Action<string> progressCallback)
    {
        try
        {
            progressCallback?.Invoke("Getting latest version...");
            string versionHash = GetLatestVersionHash(channel);
            progressCallback?.Invoke($"Latest version: {versionHash}");

            string mainFolder = GetInstallationPath();
            if (Directory.Exists(mainFolder))
                Directory.Delete(mainFolder, true);
            Directory.CreateDirectory(mainFolder);

            string channelPath = channel == "LIVE" ? HostPath : $"{HostPath}/channel/{channel.ToLower()}";
            string versionPath = $"{channelPath}/{versionHash}-";

            progressCallback?.Invoke("Fetching manifest...");
            string manifestUrl = versionPath + "rbxPkgManifest.txt";
            string manifestText;

            try
            {
                manifestText = _webClient.DownloadString(manifestUrl);
            }
            catch
            {
                channelPath = $"{HostPath}/channel/common";
                versionPath = $"{channelPath}/{versionHash}-";
                manifestText = _webClient.DownloadString(versionPath + "rbxPkgManifest.txt");
            }

            var manifestLines = manifestText.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            if (manifestLines[0] != "v0")
                throw new Exception($"Unknown manifest format: {manifestLines[0]}");

            string appSettingsPath = Path.Combine(mainFolder, "AppSettings.xml");
            File.WriteAllText(appSettingsPath, @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Settings>
    <ContentFolder>content</ContentFolder>
    <BaseUrl>http://www.roblox.com</BaseUrl>
</Settings>");
            progressCallback?.Invoke("AppSettings.xml created");

            var zipPackages = manifestLines.Where(l => l.EndsWith(".zip")).ToList();

            foreach (var packageName in zipPackages)
            {
                progressCallback?.Invoke($"Processing {packageName}...");
                string packageUrl = versionPath + packageName;

                if (ExtractPaths.TryGetValue(packageName, out string relativePath))
                {
                    string outputPath = string.IsNullOrEmpty(relativePath) ? mainFolder : Path.Combine(mainFolder, relativePath);
                    Directory.CreateDirectory(outputPath);

                    byte[] zipData = _webClient.DownloadData(packageUrl);
                    string tempZip = Path.GetTempFileName();
                    File.WriteAllBytes(tempZip, zipData);

                    try
                    {
                        ManualExtractZip(tempZip, outputPath);
                        progressCallback?.Invoke($"Extracted {packageName} to {relativePath}");
                    }
                    finally
                    {
                        File.Delete(tempZip);
                    }
                }
                else
                {
                    string destPath = Path.Combine(mainFolder, packageName);
                    byte[] data = _webClient.DownloadData(packageUrl);
                    File.WriteAllBytes(destPath, data);
                    progressCallback?.Invoke($"Saved {packageName}");
                }
            }

            progressCallback?.Invoke("Creating folder structure...");
            CreateFolderStructure(mainFolder);

            string exePath = Path.Combine(mainFolder, "RobloxPlayerBeta.exe");
            if (!File.Exists(exePath))
            {
                string[] exeFiles = Directory.GetFiles(mainFolder, "*.exe");
                if (exeFiles.Length > 0)
                    File.Copy(exeFiles[0], exePath, true);
            }

            progressCallback?.Invoke("Installation complete!");
            return File.Exists(exePath);
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke($"Error: {ex.Message}");
            return false;
        }
    }

    private static void CreateFolderStructure(string mainFolder)
    {
        string[] folders = {
            "content", "content/avatar", "content/configs", "content/fonts", "content/sky",
            "content/sounds", "content/textures", "content/models",
            "PlatformContent/pc/fonts", "PlatformContent/pc/shared_compression_dictionaries",
            "PlatformContent/pc/terrain", "PlatformContent/pc/textures",
            "ExtraContent/LuaPackages", "ExtraContent/translations",
            "ExtraContent/models", "ExtraContent/textures", "ExtraContent/places",
            "shaders", "ssl", "WebView2RuntimeInstaller"
        };

        foreach (string folder in folders)
        {
            string fullPath = Path.Combine(mainFolder, folder);
            Directory.CreateDirectory(fullPath);
        }
    }

    private static void ManualExtractZip(string zipFilePath, string extractTo)
    {
        using (var file = File.OpenRead(zipFilePath))
        {
            var entries = ParseZipEntries(file);
            foreach (var entry in entries)
            {
                if (entry.IsDirectory) continue;

                string fullPath = Path.Combine(extractTo, entry.FileName.Replace('/', Path.DirectorySeparatorChar));
                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                file.Position = entry.DataOffset;
                byte[] compressedData = new byte[entry.CompressedSize];
                file.Read(compressedData, 0, compressedData.Length);

                byte[] decompressed;
                if (entry.CompressionMethod == 0)
                {
                    decompressed = compressedData;
                }
                else if (entry.CompressionMethod == 8)
                {
                    using (var input = new MemoryStream(compressedData))
                    using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                    using (var output = new MemoryStream())
                    {
                        deflate.CopyTo(output);
                        decompressed = output.ToArray();
                    }
                }
                else
                {
                    throw new NotSupportedException($"Unsupported compression method: {entry.CompressionMethod}");
                }

                File.WriteAllBytes(fullPath, decompressed);
            }
        }
    }

    private static List<ZipEntry> ParseZipEntries(Stream zipStream)
    {
        var entries = new List<ZipEntry>();
        const uint LocalFileHeaderSignature = 0x04034b50;
        const uint CentralDirectorySignature = 0x02014b50;

        while (zipStream.Position < zipStream.Length)
        {
            uint signature = ReadUInt32LE(zipStream);
            if (signature == CentralDirectorySignature) break;
            if (signature != LocalFileHeaderSignature)
                throw new InvalidDataException("Invalid ZIP local file header signature");

            ushort version = ReadUInt16LE(zipStream);
            ushort flags = ReadUInt16LE(zipStream);
            ushort compression = ReadUInt16LE(zipStream);
            ushort modTime = ReadUInt16LE(zipStream);
            ushort modDate = ReadUInt16LE(zipStream);
            uint crc32 = ReadUInt32LE(zipStream);
            uint compressedSize = ReadUInt32LE(zipStream);
            uint uncompressedSize = ReadUInt32LE(zipStream);
            ushort fileNameLength = ReadUInt16LE(zipStream);
            ushort extraFieldLength = ReadUInt16LE(zipStream);

            byte[] fileNameBytes = new byte[fileNameLength];
            zipStream.Read(fileNameBytes, 0, fileNameLength);
            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            bool isDir = fileName.EndsWith("/") || fileName.EndsWith("\\");

            zipStream.Position += extraFieldLength;

            long dataOffset = zipStream.Position;
            zipStream.Position += compressedSize;

            entries.Add(new ZipEntry
            {
                FileName = fileName,
                IsDirectory = isDir,
                CompressionMethod = compression,
                CompressedSize = compressedSize,
                DataOffset = dataOffset
            });
        }

        return entries;
    }

    private static uint ReadUInt32LE(Stream stream)
    {
        byte[] buffer = new byte[4];
        stream.Read(buffer, 0, 4);
        return BitConverter.ToUInt32(buffer, 0);
    }

    private static ushort ReadUInt16LE(Stream stream)
    {
        byte[] buffer = new byte[2];
        stream.Read(buffer, 0, 2);
        return BitConverter.ToUInt16(buffer, 0);
    }

    private class ZipEntry
    {
        public string FileName;
        public bool IsDirectory;
        public ushort CompressionMethod;
        public uint CompressedSize;
        public long DataOffset;
    }
}