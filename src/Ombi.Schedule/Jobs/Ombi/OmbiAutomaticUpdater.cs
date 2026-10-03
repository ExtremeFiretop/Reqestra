using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ombi.Core.Processor;
using Ombi.Core.Settings;
using Ombi.Helpers;
using Ombi.Hubs;
using Ombi.Schedule.Processor;
using Ombi.Settings.Settings.Models;
using Ombi.Store.Entities;
using Ombi.Store.Repository;
using Ombi.Updater;
using OctokitApiException = Octokit.ApiException;
using Quartz;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Ombi.Schedule.Jobs.Ombi
{
    public class OmbiAutomaticUpdater : IOmbiAutomaticUpdater
    {
        public OmbiAutomaticUpdater(ILogger<OmbiAutomaticUpdater> log, IChangeLogProcessor service,
            ISettingsService<UpdateSettings> s, IProcessProvider proc, IApplicationConfigRepository appConfig,
            INotificationHubService notificationHubService)
        {
            Logger = log;
            Processor = service;
            Settings = s;
            _processProvider = proc;
            _appConfig = appConfig;
            _notificationHubService = notificationHubService;
        }

        private ILogger<OmbiAutomaticUpdater> Logger { get; }
        private IChangeLogProcessor Processor { get; }
        private ISettingsService<UpdateSettings> Settings { get; }
        private readonly IProcessProvider _processProvider;
        private readonly IApplicationConfigRepository _appConfig;
        private readonly INotificationHubService _notificationHubService;

        public string[] GetVersion()
        {
            var productVersion = AssemblyHelper.GetRuntimeVersion();
            var productArray = productVersion.Split('-');
            return productArray;
        }
        public async Task<bool> UpdateAvailable(string currentVersion)
        {
            var updates = await Processor.Process();

            // GitHub release tags include a leading "v" (for example v4.60.37),
            // while AssemblyHelper.GetRuntimeVersion() returns the numeric runtime
            // version (for example 4.60.37). Compare parsed versions rather than
            // raw strings so an equal version is not incorrectly reported as an
            // available update.
            if (TryParseVersion(updates.UpdateVersionString, out var serverVersion) &&
                TryParseVersion(currentVersion, out var installedVersion))
            {
                return serverVersion > installedVersion;
            }

            // ChangeLogProcessor already performs a semantic version comparison
            // against the running assembly. Fall back to that result if either
            // supplied version cannot be parsed.
            return updates.UpdateAvailable;
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim().TrimStart('v', 'V');
            var suffixIndex = normalized.IndexOf('-');
            if (suffixIndex >= 0)
            {
                normalized = normalized.Substring(0, suffixIndex);
            }

            return Version.TryParse(normalized, out version);
        }

        private static bool IsTransientUpdateCheckException(Exception exception)
        {
            if (exception is HttpRequestException || exception is TaskCanceledException)
            {
                return true;
            }

            // Octokit converts temporary GitHub HTTP responses into ApiException rather than
            // HttpRequestException. Treat rate limiting, request timeouts and server-side
            // failures as retryable for the scheduled update check.
            if (exception is OctokitApiException apiException)
            {
                var statusCode = (int)apiException.StatusCode;
                return statusCode == 403 ||
                       statusCode == 408 ||
                       statusCode == 429 ||
                       (statusCode >= 500 && statusCode <= 599);
            }

            return exception.InnerException != null &&
                   IsTransientUpdateCheckException(exception.InnerException);
        }

        public async Task Execute(IJobExecutionContext job)
        {
            Logger.LogDebug(LoggingEvents.Updater, "Starting Update job");

            var settings = await Settings.GetSettingsAsync();
            if (!settings.AutoUpdateEnabled && !settings.TestMode)
            {
                Logger.LogDebug(LoggingEvents.Updater, "Auto update is not enabled");
                return;
            }

            var currentLocation = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            Logger.LogDebug(LoggingEvents.Updater, "Path: {0}", currentLocation);

            var productVersion = AssemblyHelper.GetRuntimeVersion();
            Logger.LogDebug(LoggingEvents.Updater, "Product Version {0}", productVersion);
            var serverVersion = string.Empty;
            try
            {
                var productArray = GetVersion();
                var version = productArray.FirstOrDefault() ?? productVersion;
                Logger.LogDebug(LoggingEvents.Updater, "Version {0}", version);

                // Runtime versions are no longer guaranteed to contain the old
                // "version-branch" suffix. Older updater code unconditionally
                // accessed productArray[1], which crashes with IndexOutOfRangeException
                // for normal versions such as "4.60.37". The configured update
                // branch is already handled by ChangeLogProcessor, so no branch
                // token is needed here.
                Logger.LogDebug(LoggingEvents.Updater, "Looking for updates now");
                UpdateModel updates;
                try
                {
                    updates = await Processor.Process();
                }
                catch (Exception e) when (IsTransientUpdateCheckException(e))
                {
                    // A scheduled update check should not fail the Quartz job just because
                    // GitHub, DNS or the local network is temporarily unavailable. Log one
                    // concise warning and let the next scheduled run try again.
                    Logger.LogWarning(LoggingEvents.Updater,
                        "Reqestra could not reach GitHub while checking for updates: {0}. " +
                        "The updater will retry at the next scheduled check.",
                        e.Message);
                    Logger.LogDebug(e, "Transient Reqestra update-check failure");
                    return;
                }
                Logger.LogDebug(LoggingEvents.Updater, "Updates: {0}", updates);


                serverVersion = updates.UpdateVersionString;

                Logger.LogDebug(LoggingEvents.Updater, "Service Version {0}", updates.UpdateVersionString);


                // Use ChangeLogProcessor's semantic Version comparison. A raw
                // string comparison would treat "v4.60.37" and "4.60.37" as
                // different and could repeatedly trigger the updater even when the
                // installed version is already current.
                if (updates.UpdateAvailable || settings.TestMode)
                {
                    try
                    {
                        var displayVersion = serverVersion?.TrimStart('v', 'V');
                        await _notificationHubService.SendNotificationToAdmins($"Reqestra update available: v{displayVersion}. Downloading...");
                    }
                    catch (Exception notifyEx)
                    {
                        Logger.LogWarning(notifyEx, "Failed to send updater start notification");
                    }

                    // Reqestra release assets use the RID-based names produced by build.yml.
                    // Match the exact artifact for this OS/architecture instead of the old Ombi
                    // names (for example windows.* / linux.*), which no longer exist.
                    var desc = RuntimeInformation.OSDescription;
                    var process = RuntimeInformation.ProcessArchitecture;
                    var expectedAssetName = GetReleaseAssetName(process);

                    Logger.LogDebug(LoggingEvents.Updater, "OS Information: {0} {1}", desc, process);
                    if (expectedAssetName.IsNullOrEmpty())
                    {
                        Logger.LogWarning(LoggingEvents.Updater,
                            "Reqestra does not publish an automatic-update artifact for this platform: {0} {1}",
                            desc, process);
                        return;
                    }

                    var download = updates.Downloads.FirstOrDefault(x =>
                        string.Equals(x.Name, expectedAssetName, StringComparison.OrdinalIgnoreCase));
                    if (download == null)
                    {
                        Logger.LogWarning(LoggingEvents.Updater,
                            "Reqestra release did not contain the expected update artifact {0}",
                            expectedAssetName);
                        return;
                    }

                    Logger.LogDebug(LoggingEvents.Updater, "Found the download! {0}", download.Name);
                    Logger.LogDebug(LoggingEvents.Updater, "URL {0}", download.Url);

                    Logger.LogDebug(LoggingEvents.Updater, "Clearing out Temp Path");
                    var tempPath = Path.Combine(currentLocation, "TempUpdate");
                    if (Directory.Exists(tempPath))
                    {
                        DeleteDirectory(tempPath);
                    }

                    // Temp Path
                    Directory.CreateDirectory(tempPath);


                    if (settings.UseScript && !settings.WindowsService)
                    {
                        RunScript(settings, download.Url);
                        return;
                    }

                    // Download it
                    Logger.LogDebug(LoggingEvents.Updater, "Downloading the file {0} from {1}", download.Name, download.Url);
                    var extension = download.Name.Split('.').Last();
                    var zipDir = Path.Combine(currentLocation, $"Ombi.{extension}");
                    Logger.LogDebug(LoggingEvents.Updater, "Zip Dir: {0}", zipDir);
                    try
                    {
                        if (File.Exists(zipDir))
                        {
                            File.Delete(zipDir);
                        }

                        Logger.LogDebug(LoggingEvents.Updater, "Starting Download");
                        await DownloadAsync(download.Url, zipDir);
                        Logger.LogDebug(LoggingEvents.Updater, "Finished Download");
                    }
                    catch (Exception e)
                    {
                        Logger.LogDebug(LoggingEvents.Updater, "Error when downloading");
                        Logger.LogDebug(LoggingEvents.Updater, e.Message);
                        Logger.LogError(LoggingEvents.Updater, e, "Error when downloading the zip");
                        throw;
                    }

                    // Extract it
                    Logger.LogDebug(LoggingEvents.Updater, "Extracting ZIP");
                    Extract(zipDir, tempPath);

                    Logger.LogDebug(LoggingEvents.Updater, "Finished Extracting files");
                    Logger.LogDebug(LoggingEvents.Updater, "Starting the Ombi.Updater process");
                    var updaterExtension = string.Empty;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        updaterExtension = ".exe";
                    }
                    var updaterFile = Path.Combine(tempPath, "updater", $"Ombi.Updater{updaterExtension}");
                    if (!File.Exists(updaterFile))
                    {
                        throw new FileNotFoundException(
                            "The Reqestra release does not contain the packaged updater executable.",
                            updaterFile);
                    }

                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        File.SetUnixFileMode(updaterFile,
                            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                    }

                    // There must be an update
                    var start = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true, // Ignored if UseShellExecute is set to true
                        FileName = updaterFile,
                        Arguments = GetArgs(settings),
                        WorkingDirectory = tempPath,
                    };
                    //if (settings.Username.HasValue())
                    //{
                    //    start.UserName = settings.Username;
                    //}
                    //if (settings.Password.HasValue())
                    //{
                    //    start.Password = settings.Password.ToSecureString();
                    //}
                    using (var proc = new Process { StartInfo = start })
                    {
                        proc.Start();
                    }


                    Logger.LogDebug(LoggingEvents.Updater, "Bye bye");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception thrown in the OmbiUpdater, see previous messages");
                try
                {
                    await _notificationHubService.SendNotificationToAdmins("Ombi auto-update failed. Check logs for details.");
                }
                catch (Exception notifyEx)
                {
                    Logger.LogWarning(notifyEx, "Failed to send updater failure notification");
                }
                throw;
            }
        }

        private static string GetReleaseAssetName(Architecture architecture)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return architecture switch
                {
                    Architecture.X64 => "win-x64.zip",
                    Architecture.X86 => "win-x86.zip",
                    _ => null,
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return architecture == Architecture.X64 ? "osx-x64.tar.gz" : null;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return architecture switch
                {
                    Architecture.X64 => "linux-x64.tar.gz",
                    Architecture.Arm => "linux-arm.tar.gz",
                    Architecture.Arm64 => "linux-arm64.tar.gz",
                    _ => null,
                };
            }

            return null;
        }

        private string GetArgs(UpdateSettings settings)
        {
            var url = _appConfig.Get(ConfigurationTypes.Url);
            var storage = _appConfig.Get(ConfigurationTypes.StoragePath);

            var currentLocation = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            var processName = settings.ProcessName.HasValue() ? settings.ProcessName : "Ombi";
            var processId = _processProvider.GetCurrentProcessId();

            var sb = new StringBuilder();
            sb.Append($"--applicationPath \"{currentLocation}\" --processname \"{processName}\" --processId {processId} ");

            if (settings.WindowsService)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new InvalidOperationException("Windows service update mode can only be used on Windows.");
                }

                if (!settings.WindowsServiceName.HasValue())
                {
                    throw new InvalidOperationException(
                        "Windows service update mode is enabled but no Windows service name is configured.");
                }

                sb.Append($"--windowsServiceName \"{settings.WindowsServiceName}\" ");
            }

            // Preserve the arguments used by non-service installations when the updater
            // restarts Reqestra. The old implementation built these arguments in a second
            // StringBuilder that was never returned.
            if (url?.Value.HasValue() ?? false)
            {
                sb.Append($"--host \"{url.Value}\" ");
            }
            if (storage?.Value.HasValue() ?? false)
            {
                sb.Append($"--storage \"{storage.Value}\" ");
            }

            return sb.ToString().Trim();
        }

        private void RunScript(UpdateSettings settings, string downloadUrl)
        {
            var scriptToRun = settings?.ScriptLocation ?? string.Empty;
            if (scriptToRun.IsNullOrEmpty())
            {
                Logger.LogError("Use Script is enabled but there is no script to run");
                return;
            }

            if (!File.Exists(scriptToRun))
            {
                Logger.LogError("Cannot find the file {0}", scriptToRun);
                return;
            }

            _processProvider.Start(scriptToRun, downloadUrl + " " + GetArgs(settings));

            Logger.LogDebug(LoggingEvents.Updater, "Script started");
        }

        private void Extract(string zipDir, string tempPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var files = ZipFile.OpenRead(zipDir))
                {
                    foreach (var entry in files.Entries)
                    {
                        if (entry.FullName.Contains("/"))
                        {
                            var path = Path.GetDirectoryName(Path.Combine(tempPath, entry.FullName));
                            Directory.CreateDirectory(path);
                        }

                        entry.ExtractToFile(Path.Combine(tempPath, entry.FullName));
                    }
                }
            }
            else
            {
                // Something else!
                using (var stream = File.Open(zipDir, FileMode.Open))
                using (var files = ReaderFactory.OpenReader(stream))
                {
                    Directory.CreateDirectory(tempPath);
                    files.WriteAllToDirectory(tempPath, new ExtractionOptions { Overwrite = true });
                }
            }
        }

        public async Task DownloadAsync(string requestUri, string filename)
        {
            Logger.LogDebug(LoggingEvents.Updater, "Starting the DownloadAsync");
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            using (var client = new WebClient())
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            {
                await client.DownloadFileTaskAsync(requestUri, filename);
            }
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                //Settings?.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// </summary>
        public static void DeleteDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        public static void ExecLinuxCommand(string cmd)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

            process.Start();
            process.WaitForExit();
        }
    }
}