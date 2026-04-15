using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Windows;

internal static class VcRuntimeBootstrapper
{
    private const string Title = "VoiceCraft";
    private const uint MbIconError = 0x10;
    private const uint MbYesNo = 0x04;
    private const uint MbSystemModal = 0x1000;
    private const int IdYes = 6;

    public static bool EnsureInstalledOrPrompt()
    {
        if (IsInstalled())
            return true;

        var architecture = GetRedistributableArchitecture();
        var installerPath = GetBundledInstallerPath(architecture);
        var source = installerPath is null
            ? "download the latest Microsoft Visual C++ Redistributable"
            : $"install the bundled Microsoft Visual C++ Redistributable ({Path.GetFileName(installerPath)})";

        var result = MessageBoxW(
            IntPtr.Zero,
            $"VoiceCraft requires the Microsoft Visual C++ Redistributable ({architecture}) to load the Opus codec.\n\n" +
            $"Please {source}, then launch VoiceCraft again.\n\n" +
            "Press Yes to start the installer now, or No to close VoiceCraft.",
            Title,
            MbYesNo | MbIconError | MbSystemModal);

        if (result != IdYes)
            return false;

        try
        {
            StartInstaller(architecture, installerPath);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            MessageBoxW(
                IntPtr.Zero,
                "VoiceCraft could not start the Visual C++ Redistributable installer automatically.\n\n" +
                $"Please install it manually from:\n{GetDownloadUrl(architecture)}",
                Title,
                MbIconError | MbSystemModal);
        }

        return false;
    }

    private static bool IsInstalled()
    {
        var architecture = GetRedistributableArchitecture();
        return architecture switch
        {
            "arm64" => IsInstalled("arm64") || IsInstalled("x64"),
            _ => IsInstalled(architecture)
        };
    }

    private static bool IsInstalled(string architecture)
    {
        const string baseKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\";
        return IsInstalled(RegistryView.Registry64, baseKey + architecture) ||
               IsInstalled(RegistryView.Registry32, baseKey + architecture);
    }

    private static bool IsInstalled(RegistryView view, string keyPath)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var key = baseKey.OpenSubKey(keyPath);
            if (key == null)
                return false;

            var installedValue = key.GetValue("Installed");
            return installedValue switch
            {
                int i => i == 1,
                long l => l == 1,
                uint u => u == 1,
                string s when int.TryParse(s, out var parsed) => parsed == 1,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static string GetRedistributableArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => "x64"
        };
    }

    private static string? GetBundledInstallerPath(string architecture)
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"vc_redist.{architecture}.exe");
        return File.Exists(path) ? path : null;
    }

    private static void StartInstaller(string architecture, string? installerPath)
    {
        var fileName = installerPath ?? GetDownloadUrl(architecture);
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true
        });
    }

    private static string GetDownloadUrl(string architecture)
    {
        return architecture switch
        {
            "x86" => "https://aka.ms/vc14/vc_redist.x86.exe",
            "arm64" => "https://aka.ms/vc14/vc_redist.arm64.exe",
            _ => "https://aka.ms/vc14/vc_redist.x64.exe"
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
