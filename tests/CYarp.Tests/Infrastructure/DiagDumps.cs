using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;

namespace CYarp.Tests.Infrastructure;

/// <summary>
/// Helper class for creating diagnostic dumps during long-running tests
/// Based on Microsoft.Diagnostics.NETCore.Client for programmatic dump creation
/// </summary>
public static class DiagDumps
{
    public static string ArtifactsDir =>
        Path.Combine(AppContext.BaseDirectory, "diag"); // Goes to bin/..../diag

    public static void EnsureDir() => Directory.CreateDirectory(ArtifactsDir);

    public static string DumpPath(string tag) =>
        Path.Combine(ArtifactsDir, $"{Safe(tag)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_full.dmp");

    public static string GcDumpPath(string tag) =>
        Path.Combine(ArtifactsDir, $"{Safe(tag)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.gcdump");

    static string Safe(string s) => string.Join("_", s.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Write a full process dump (includes all memory, useful for debugging)
    /// </summary>
    public static void WriteFullDump(string tag)
    {
        try
        {
            EnsureDir();
            var pid = Process.GetCurrentProcess().Id;
            var client = new DiagnosticsClient(pid);
            var dumpPath = DumpPath(tag);
            client.WriteDump(DumpType.Full, dumpPath, logDumpGeneration: true);
            Console.WriteLine($"[DiagDump] Full dump written to: {dumpPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DiagDump] Failed to write full dump: {ex.Message}");
        }
    }

    /// <summary>
    /// Write a GC heap dump (smaller, focused on managed heap for leak detection)
    /// This launches dotnet-gcdump tool out-of-process
    /// </summary>
    public static async Task WriteGcDumpAsync(string tag, CancellationToken ct = default)
    {
        try
        {
            EnsureDir();
            var pid = Process.GetCurrentProcess().Id;
            var outPath = GcDumpPath(tag);

            // Launch dotnet-gcdump tool
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"gcdump collect -p {pid} -o \"{outPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine("[DiagDump] Failed to start gcdump process");
                return;
            }

            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode == 0)
            {
                Console.WriteLine($"[DiagDump] GC dump written to: {outPath}");
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                Console.WriteLine($"[DiagDump] gcdump failed: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DiagDump] Failed to write GC dump: {ex.Message}");
        }
    }

    /// <summary>
    /// Write heap snapshot using DiagnosticsClient (portable, no external tool needed)
    /// </summary>
    public static async Task WriteHeapDumpAsync(string tag, CancellationToken ct = default)
    {
        try
        {
            EnsureDir();
            var pid = Process.GetCurrentProcess().Id;
            var client = new DiagnosticsClient(pid);
            var gcDumpPath = GcDumpPath(tag);

            using var fs = new FileStream(gcDumpPath, FileMode.Create, FileAccess.Write);
            await Task.Run(() => client.WriteDump(DumpType.WithHeap, gcDumpPath, logDumpGeneration: true), ct);
            
            Console.WriteLine($"[DiagDump] Heap dump written to: {gcDumpPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DiagDump] Failed to write heap dump: {ex.Message}");
        }
    }
}
