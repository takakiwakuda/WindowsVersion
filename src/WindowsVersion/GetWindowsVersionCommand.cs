using System;
using System.IO;
using System.Management.Automation;
using System.Security;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;

namespace WindowsVersion;

/// <summary>
/// The Get-WindowsVersion cmdlet retrieves Windows version information.
/// </summary>
[Cmdlet(VerbsCommon.Get, "WindowsVersion",
        HelpUri = "https://github.com/takakiwakuda/WindowsVersion/blob/main/src/WindowsVersion/doc/Get-WindowsVersion.md")]
[OutputType(typeof(WindowsVersionInfo))]
public sealed class GetWindowsVersionCommand : PSCmdlet
{
    /// <summary>
    /// Retrieves Windows version information and writes it to the pipe.
    /// </summary>
    protected override void ProcessRecord()
    {
        WriteObject(GetWindowsVersion());
    }

    /// <summary>
    /// Retrieves the build of Windows Feature Experience Pack.
    /// </summary>
    /// <returns>A string representing the build of Windows Feature Experience Pack.</returns>
    private Version? GetExperienceVersion()
    {
        string fileName = @"C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\AppxManifest.xml";
        if (!File.Exists(fileName))
        {
            WriteWarning("Windows Feature Experience Pack is not probably installed.");
            return null;
        }

        XElement package;
        try
        {
            using FileStream stream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            package = XElement.Load(stream, LoadOptions.None);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
        {
            ErrorRecord errorRecord = new(ex, "AccessDenied", ErrorCategory.PermissionDenied, fileName);
            WriteError(errorRecord);
            return null;
        }

        XName identity = XName.Get("Identity", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
        string? version = package.Element(identity)?.Attribute("Version")?.Value;
        if (string.IsNullOrEmpty(version))
        {
            WriteWarning("For some reason, the version of Windows Feature Experience Pack could not be retrieved.");
            return null;
        }

        return Version.Parse(version);
    }

    /// <summary>
    /// Retrieves Windows version information.
    /// </summary>
    /// <returns>Windows version information.</returns>
    private WindowsVersionInfo GetWindowsVersion()
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");

        if (key is null)
        {
            WriteWarning("Your computer is probably broken because the registry is missing.");
            return new WindowsVersionInfo();
        }

        string? version = (string?)key.GetValue("DisplayVersion");
        if (version is null)
        {
            WriteDebug("DisplayVersion was not found.");
            version = (string?)key.GetValue("ReleaseId");
        }

        int? seconds = (int?)key.GetValue("InstallDate");
        DateTime? installDate = null;
        if (seconds is not null)
        {
            installDate = DateTimeOffset.FromUnixTimeSeconds(seconds.Value).DateTime;
        }

        StringBuilder osBuild = new(16);
        string? currentBuild = (string?)key.GetValue("CurrentBuild");
        if (currentBuild is not null)
        {
            osBuild.Append(currentBuild);

            int? ubr = (int?)key.GetValue("UBR");
            if (ubr is not null)
            {
                if (osBuild.Length != 0)
                {
                    osBuild.Append('.');
                }
                osBuild.Append(ubr);
            }
        }

        return new WindowsVersionInfo()
        {
            Edition = (string?)key.GetValue("ProductName"),
            Version = version,
            InstallDate = installDate,
            OSBuild = osBuild.Length == 0 ? null : osBuild.ToString(),
            Experience = GetExperienceVersion()
        };
    }
}

/// <summary>
/// Provides information about the version of Windows.
/// </summary>
public sealed class WindowsVersionInfo
{
    /// <summary>
    /// Gets or sets the edition of Windows.
    /// </summary>
    public string? Edition { get; init; }

    /// <summary>
    /// Gets or sets the version of Windows.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets or sets the installation date of Windows.
    /// </summary>
    public DateTime? InstallDate { get; init; }

    /// <summary>
    /// Gets or sets the build of Windows.
    /// </summary>
    public string? OSBuild { get; init; }

    /// <summary>
    /// Gets or set the version of Windows Feature Experience Pack.
    /// </summary>
    public Version? Experience { get; init; }

    /// <summary>
    /// Returns a string representing the Windows version.
    /// </summary>
    /// <returns>A string representing the Windows version.</returns>
    public override string ToString()
    {
        StringBuilder builder = new(64);
        if (Edition is not null)
        {
            builder.Append(Edition);
        }

        if (Version is not null)
        {
            if (builder.Length != 0)
            {
                builder.Append(' ');
            }
            builder.Append("Version ");
            builder.Append(Version);
        }

        if (OSBuild is not null)
        {
            if (builder.Length != 0)
            {
                builder.Append(' ');
            }
            builder.Append("(OS Build ");
            builder.Append(OSBuild);
            builder.Append(')');
        }

        return builder.ToString();
    }
}
