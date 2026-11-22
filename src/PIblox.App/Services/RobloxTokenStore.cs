using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace PIblox.App.Services;

internal sealed record RobloxOAuthTokens(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAtUtc);

internal static class RobloxTokenStore
{
    private static readonly string TokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PIblox", "roblox-oauth.dat");

    public static void Save(RobloxOAuthTokens tokens)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TokenPath)!);
        var clearText = JsonSerializer.SerializeToUtf8Bytes(tokens);
        try
        {
            var encrypted = Protect(clearText);
            var temporaryPath = TokenPath + ".tmp";
            File.WriteAllBytes(temporaryPath, encrypted);
            File.Move(temporaryPath, TokenPath, overwrite: true);
        }
        finally { CryptographicOperations.ZeroMemory(clearText); }
    }

    public static RobloxOAuthTokens? Load()
    {
        if (!File.Exists(TokenPath)) return null;
        try
        {
            var protectedBytes = File.ReadAllBytes(TokenPath);
            var clearText = Unprotect(protectedBytes);
            try { return JsonSerializer.Deserialize<RobloxOAuthTokens>(clearText); }
            finally { CryptographicOperations.ZeroMemory(clearText); }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException or Win32Exception)
        {
            return null;
        }
    }

    public static void Delete()
    {
        try { if (File.Exists(TokenPath)) File.Delete(TokenPath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static byte[] Protect(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        var input = new DataBlob { Size = bytes.Length, Data = pointer };
        try
        {
            if (!CryptProtectData(ref input, "PIblox Roblox OAuth", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0x1, out var output))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var result = new byte[output.Size];
                Marshal.Copy(output.Data, result, 0, output.Size);
                return result;
            }
            finally { if (output.Data != IntPtr.Zero) LocalFree(output.Data); }
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    private static byte[] Unprotect(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        var input = new DataBlob { Size = bytes.Length, Data = pointer };
        try
        {
            if (!CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0x1, out var output))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var result = new byte[output.Size];
                Marshal.Copy(output.Data, result, 0, output.Size);
                return result;
            }
            finally { if (output.Data != IntPtr.Zero) LocalFree(output.Data); }
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob { public int Size; public IntPtr Data; }

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("Crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr memory);
}