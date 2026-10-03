using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace Ombi.Updater
{
    public class ProcessProvider : IProcessProvider
    {
        public ProcessProvider()
        {
            
        }

        public int GetCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        public ProcessInfo GetCurrentProcess()
        {
            return ConvertToProcessInfo(Process.GetCurrentProcess());
        }
        public bool Exists(int processId)
        {
            return GetProcessById(processId) != null;
        }

        public bool Exists(string processName)
        {
            return GetProcessesByName(processName).Any();
        }
        public ProcessInfo GetProcessById(int id)
        {
            Console.WriteLine("Finding process with Id:{0}", id);

            var processInfo = ConvertToProcessInfo(Process.GetProcesses().FirstOrDefault(p => p.Id == id));

            if (processInfo == null)
            {
                Console.WriteLine("Unable to find process with ID {0}", id);
            }
            else
            {
                Console.WriteLine("Found process {0}", processInfo.ToString());
            }

            return processInfo;
        }

        public List<ProcessInfo> FindProcessByName(string name)
        {
            return GetProcessesByName(name).Select(ConvertToProcessInfo).Where(c => c != null).ToList();
        }
        

        public void WaitForExit(Process process)
        {
            Console.WriteLine("Waiting for process {0} to exit.", process.ProcessName);

            process.WaitForExit();
        }

        public void SetPriority(int processId, ProcessPriorityClass priority)
        {
            var process = Process.GetProcessById(processId);

            Console.WriteLine("Updating [{0}] process priority from {1} to {2}",
                        process.ProcessName,
                        process.PriorityClass,
                        priority);

            process.PriorityClass = priority;
        }

        public bool Kill(StartupOptions opts)
        {
            if (opts == null)
            {
                throw new ArgumentNullException(nameof(opts));
            }

            if (opts.IsWindowsService)
            {
                return StopWindowsService(opts.WindowsServiceName);
            }

            Process process;
            if (opts.OmbiProcessId > 0)
            {
                try
                {
                    process = Process.GetProcessById(opts.OmbiProcessId);
                }
                catch (ArgumentException)
                {
                    // The exact process handed to the updater has already exited. This is
                    // equivalent to a successful stop and it is safe to continue replacing files.
                    Console.WriteLine("Process with id {0} has already exited", opts.OmbiProcessId);
                    return true;
                }

                // Protect against the extremely small chance that the PID was recycled before
                // the updater started. Never kill a different process merely because it now has
                // the PID we were given.
                if (!string.IsNullOrWhiteSpace(opts.ProcessName) &&
                    !string.Equals(process.ProcessName, opts.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(
                        "Process id {0} belongs to '{1}', not expected process '{2}'. Update aborted.",
                        opts.OmbiProcessId, process.ProcessName, opts.ProcessName);
                    return false;
                }
            }
            else
            {
                // Backward compatibility for manually invoked/older updater callers that do
                // not provide a PID. New Reqestra builds always pass the exact current PID.
                process = Process.GetProcesses().FirstOrDefault(p =>
                    string.Equals(p.ProcessName, opts.ProcessName, StringComparison.OrdinalIgnoreCase));

                if (process == null)
                {
                    Console.WriteLine("Cannot find process with name: {0}", opts.ProcessName);
                    return false;
                }
            }

            if (process.Id <= 0)
            {
                return false;
            }

            Console.WriteLine("[{0}]: Killing process {1}", process.Id, process.ProcessName);
            process.Kill();
            Console.WriteLine("[{0}]: Waiting for exit", process.Id);
            process.WaitForExit();
            Console.WriteLine("[{0}]: Process terminated successfully", process.Id);
            return true;
        }

        public bool StartService(string serviceName)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("Windows services can only be started on Windows");
                return false;
            }

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                Console.WriteLine("Cannot start a Windows service without a service name");
                return false;
            }

            try
            {
                using var service = new ServiceController(serviceName);
                service.Refresh();

                if (service.Status == ServiceControllerStatus.Running)
                {
                    Console.WriteLine("Windows service {0} is already running", serviceName);
                    return true;
                }

                if (service.Status == ServiceControllerStatus.StartPending)
                {
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    service.Refresh();
                    return service.Status == ServiceControllerStatus.Running;
                }

                if (service.Status == ServiceControllerStatus.StopPending)
                {
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    service.Refresh();
                }

                if (service.Status == ServiceControllerStatus.Paused)
                {
                    Console.WriteLine("Continuing Windows service {0}", serviceName);
                    service.Continue();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    service.Refresh();
                    return service.Status == ServiceControllerStatus.Running;
                }

                Console.WriteLine("Starting Windows service {0}", serviceName);
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                service.Refresh();

                var started = service.Status == ServiceControllerStatus.Running;
                Console.WriteLine(started
                    ? "Windows service {0} started successfully"
                    : "Windows service {0} did not reach the Running state", serviceName);
                return started;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to start Windows service {0}: {1}", serviceName, e.Message);
                return false;
            }
        }

        private static bool StopWindowsService(string serviceName)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("Windows services can only be stopped on Windows");
                return false;
            }

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                Console.WriteLine("Cannot stop a Windows service without a service name");
                return false;
            }

            try
            {
                using var service = new ServiceController(serviceName);
                service.Refresh();

                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine("Windows service {0} is already stopped", serviceName);
                    return true;
                }

                if (service.Status == ServiceControllerStatus.StopPending)
                {
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    service.Refresh();
                    return service.Status == ServiceControllerStatus.Stopped;
                }

                if (!service.CanStop)
                {
                    Console.WriteLine("Windows service {0} cannot be stopped", serviceName);
                    return false;
                }

                Console.WriteLine("Stopping Windows service {0}", serviceName);
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                service.Refresh();

                var stopped = service.Status == ServiceControllerStatus.Stopped;
                Console.WriteLine(stopped
                    ? "Windows service {0} stopped successfully"
                    : "Windows service {0} did not reach the Stopped state", serviceName);
                return stopped;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to stop Windows service {0}: {1}", serviceName, e.Message);
                return false;
            }
        }

        public void KillAll(string processName)
        {
            var processes = GetProcessesByName(processName);

            Console.WriteLine("Found {0} processes to kill", processes.Count);

            foreach (var processInfo in processes)
            {
                if (processInfo.Id == Process.GetCurrentProcess().Id)
                {
                    Console.WriteLine("Tried killing own process, skipping: {0} [{1}]", processInfo.Id, processInfo.ProcessName);
                    continue;
                }

                Console.WriteLine("Killing process: {0} [{1}]", processInfo.Id, processInfo.ProcessName);
                Kill(new StartupOptions{OmbiProcessId = processInfo.Id});
            }
        }


        private ProcessInfo ConvertToProcessInfo(Process process)
        {
            if (process == null) return null;

            process.Refresh();

            ProcessInfo processInfo = null;

            try
            {
                if (process.Id <= 0) return null;

                processInfo = new ProcessInfo
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    StartPath = GetExeFileName(process)
                };

                if (process.Id != Process.GetCurrentProcess().Id && process.HasExited)
                {
                    processInfo = null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return processInfo;

        }

        private static string GetExeFileName(Process process)
        {
            return process.MainModule.FileName;
        }

        private List<Process> GetProcessesByName(string name)
        {
            var processes = Process.GetProcessesByName(name).ToList();

            Console.WriteLine("Found {0} processes with the name: {1}", processes.Count, name);

            try
            {
                foreach (var process in processes)
                {
                    Console.WriteLine(" - [{0}] {1}", process.Id, process.ProcessName);
                }
            }
            catch
            {
                // Don't crash on gettings some log data.
            }

            return processes;
        }
    }

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string StartPath { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1} [{2}]", Id, Name ?? "Unknown", StartPath ?? "Unknown");
        }
    }
}
