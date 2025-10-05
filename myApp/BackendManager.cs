using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

public static class BackendManager
{
    private static readonly string AppName = "SDApp";
    
    
    public static void EnsureBackendFromBundleMac()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Console.WriteLine("Not macOS, skipping backend copy to support directory.");
            return;
        }
        string appSupportDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName
        );

        string sourceBackend = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend");
        string sourceScripts = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setup_scripts");

        string targetBackend = Path.Combine(appSupportDir, "backend");
        string targetScripts = Path.Combine(appSupportDir, "setup_scripts");
        string markerFile = Path.Combine(appSupportDir, ".backend_installed");

        // Skip if already copied over
        if (File.Exists(markerFile))
        {
            Console.WriteLine("Backend already exists in Application Support, skipping copy.");
            return;
        }

        // Copy directories recursively
        CopyDirectory(sourceBackend, targetBackend);
        CopyDirectory(sourceScripts, targetScripts);
        
        // Write marker file
        File.WriteAllText(markerFile, DateTime.UtcNow.ToString("o"));
        Console.WriteLine($"Backend copied successfully. Marker created at {markerFile}");
        
        SetupBackendVenvMac();
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        // Recursively copy subdirectories
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    private static void RunSetupScriptMac(string scriptName, string waitFor)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Console.WriteLine("Not macOS, skipping backend launch.");
            return;
        }
        // TODO: automatically switch based on release vs dev build
        /*
        // Use bundled scripts in Application Support for release
        string scriptsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName,
            "setup_scripts"
        );
        */
        // Use local scripts in development
        string scriptsDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "../../../../setup_scripts");
        Console.WriteLine($"Using scripts directory: {scriptsDir}");

        string backendScriptPath = Path.Combine(scriptsDir, scriptName);

        if (!File.Exists(backendScriptPath))
        {
            Console.WriteLine($"Backend script not found: {backendScriptPath}");
            Console.WriteLine("You may need to run EnsureBackendAsync() first.");
            return;
        }

        // Ensure executable permissions (just in case)
        var chmod = new ProcessStartInfo
        {
            FileName = "/bin/chmod",
            Arguments = $"+x \"{backendScriptPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        Process.Start(chmod)?.WaitForExit();

        // Start backend
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"\"{backendScriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = Process.Start(startInfo);
        if (process != null)
        {
            bool isReady = false;
            int timeoutCounter = 0;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                    if (e.Data.Contains(waitFor))
                        isReady = true;
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.Error.WriteLine(e.Data);
                    if (e.Data.Contains(waitFor))
                        isReady = true;
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for backend ready signal
            while (!isReady)
            {
                Thread.Sleep(500);
                timeoutCounter++;
                if (timeoutCounter >= 500)
                    throw new TimeoutException("Backend did not start in time.");
            }
        }
        else
        {
            Console.WriteLine("Failed to start macOS backend process.");
        }
    }

    public static void SetupBackendVenvMac()
    {
        RunSetupScriptMac("setup_sdapi_venv.sh", "Setup complete!");
    }
    
    public static void DownloadDreamshaperMac()
    {
        RunSetupScriptMac("setup_sdapi_model.sh", "Exiting default model download script.");
    }

    /// <summary>
    /// Launches the backend on macOS. Waits until it prints "Application startup complete".
    /// </summary>
    public static void RunBackendMac()
    {
        RunSetupScriptMac("launch_sdapi_server.sh", "Application startup complete");
    }
    
}
