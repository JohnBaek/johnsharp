using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace JohnIsDev.Core.Features.Utils;

/// <summary>
/// SystemKeyUtil
/// </summary>
public class SystemKeyUtil()
{
    /// <summary>
    /// Retrieves a unique key for the machine based on hardware information,
    /// using platform-specific methods to extract serial numbers or identifiers.
    /// </summary>
    /// <returns>A string containing the machine's unique hardware key.</returns>
    public string GetMachineUniqueKey()
    {
        string hardwareInfo = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            hardwareInfo = GetSimpleHardwareFingerprint();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            hardwareInfo = GetMacUniqueKey().Replace("-", "").ToLower();
            
            
        }

        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hardwareInfo));
        return Convert.ToHexString(bytes).ToLower();
    }
    
    public string GetSimpleHardwareFingerprint()
    {
        string id = Environment.MachineName + Environment.UserName + RuntimeInformation.OSDescription;
        return id;
    }
    
    /// <summary>
    /// Get SystemKey
    /// </summary>
    /// <param name="className"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    private string GetWmiValue(string className, string propertyName)
    {
        string? result = string.Empty;
        try
        {
            string query = $"SELECT {propertyName} FROM {className}";
            using ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            if (searcher != null)
                throw new Exception("searcher is null");

            foreach (ManagementObject obj in searcher.Get())
            {
                result = obj[propertyName].ToString().Trim();
                if (!string.IsNullOrEmpty(result)) 
                    break;
            }

            return result;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// Get Mac UniqueKey
    /// </summary>
    /// <returns></returns>
    private string GetMacUniqueKey()
    {
        try
        {
            // Put the serial number on 'core'
            ProcessStartInfo startInfo = new ProcessStartInfo("ioreg", "-l | grep IOPlatformSerialNumber")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Get the process
            using Process? process = Process.Start(startInfo);
            if(process == null)
                throw new Exception("Process is null");
            
            // Get the output
            using StreamReader reader = process.StandardOutput;
            
            string output = reader.ReadToEnd();
            string[] parts = output.Split('"');
            
            if(parts.Length == 0 || parts[0] == "" || parts[0] == null)
                throw new Exception("No output");
           
            return parts[3].Trim();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            throw;
        }
    }
}