using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// Produces a standalone Development Player configured for Memory Profiler attachment.
/// Invoke with: -executeMethod MemoryProfileBuild.BuildWindowsPlayer
/// </summary>
public static class MemoryProfileBuild
{
    private const string OutputPath = "Builds/MemoryProfile/DanceMemoryProfile.exe";

    public static void BuildWindowsPlayer()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/1.Scene/DanceScene_1_FMOD.unity" },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Memory profiling build failed: {report.summary.result}");
    }
}
