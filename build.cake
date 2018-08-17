using System;

// Parameters.
var projectName = "Genesis.RetryWithBackoff";
var semanticVersion = "3.0.0";
var version = semanticVersion + ".0";
var configuration = EnvironmentVariable("CONFIGURATION") ?? "Release";
var nugetSource = "https://www.nuget.org/api/v2/package";

// To push to NuGet, run with: & {$env:PUSH="true"; ./build.ps1}
var push = bool.Parse(EnvironmentVariable("PUSH") ?? "false");

// Paths.
var srcDir = Directory("Src");
var solution = srcDir + File(projectName + ".sln");
var testProject = srcDir + Directory(projectName + ".UnitTests") + File(projectName + ".UnitTests" + ".csproj");
var nuGetPackage = srcDir + Directory(projectName) + Directory("bin") + Directory(configuration) + File(projectName + "." + semanticVersion + ".nupkg");

Setup(context => Information("Building {0} version {1}.", projectName, version));

Teardown(context => Information("Build {0} finished.", version));

Task("Clean")
    .Does(() => DotNetCoreClean(solution));

Task("Pre-Build")
    .Does(
        () =>
        {
            CreateAssemblyInfo(
                srcDir + File("AssemblyInfoCommon.cs"),
                new AssemblyInfoSettings
                {
                    Company = "Kent Boogaart",
                    Product = projectName,
                    Copyright = "© Copyright. Kent Boogaart.",
                    Version = version,
                    FileVersion = version,
                    InformationalVersion = semanticVersion.ToString(),
                    Configuration = configuration
                }
                .AddCustomAttribute("NeutralResourcesLanguage", "System.Resources", "en-US"));
        });

Task("Build")
    .IsDependentOn("Pre-Build")
    .Does(
        () =>
        {
            var msBuildSettings = new DotNetCoreMSBuildSettings();
            msBuildSettings.Properties["Version"] = new string[]
            {
                version,
            };

            var settings = new DotNetCoreBuildSettings
            {
                Configuration = configuration,
                MSBuildSettings = msBuildSettings,
            };

            DotNetCoreBuild(solution, settings);
        });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => DotNetCoreTest(testProject));

Task("Push")
    .IsDependentOn("Test")
    .WithCriteria(push)
    .Does(
        () =>
        {
            var settings = new DotNetCoreNuGetPushSettings
            {
                Source = nugetSource,
            };

            DotNetCoreNuGetPush(nuGetPackage, settings);
        });

Task("Default")
    .IsDependentOn("Push");

RunTarget(Argument("target", "Default"));