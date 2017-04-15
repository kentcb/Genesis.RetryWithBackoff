#I "Src/packages/FAKE/tools"
#r "FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile

// properties
let projectName = "Genesis.RetryWithBackoff"
let semanticVersion = "2.0.0"
let version = (>=>) @"(?<major>\d*)\.(?<minor>\d*)\.(?<build>\d*).*?" "${major}.${minor}.${build}.0" semanticVersion
let configuration = getBuildParamOrDefault "configuration" "Release"
let srcDir = "Src/"
let nugetSource = "https://www.nuget.org/api/v2/package"
let solutionPath = srcDir @@ projectName + ".sln"

Target "Restore" (fun _ ->
    DotNetCli.Restore (fun p ->
        { p with
            Project = solutionPath
        })
)

Target "Build" (fun _ ->
    // generate the shared assembly info
    CreateCSharpAssemblyInfoWithConfig (srcDir @@ "AssemblyInfoCommon.cs")
        [
            Attribute.Version version
            Attribute.FileVersion version
            Attribute.Configuration configuration
            Attribute.Company "Kent Boogaart"
            Attribute.Product projectName
            Attribute.Copyright "© Copyright. Kent Boogaart."
            Attribute.Trademark ""
            Attribute.Culture ""
            Attribute.StringAttribute("NeutralResourcesLanguage", "en-US", "System.Resources")
            Attribute.StringAttribute("AssemblyInformationalVersion", semanticVersion, "System.Reflection")
        ]
        (AssemblyInfoFileConfig(false))

    DotNetCli.Build (fun p ->
        { p with
            Project = solutionPath
            Configuration = configuration
            AdditionalArgs =
            [
                "/property:Version=" + semanticVersion
            ]
        })
)

Target "Test" (fun _ ->
    let unitTestProjectDir = srcDir @@ projectName + ".UnitTests"
    let result = Shell.Exec("dotnet", "xunit -configuration " + configuration, unitTestProjectDir)

    if result <> 0 then failwithf "Tests failed with exit code %d" result
)

Target "Push" (fun _ ->
    let packagePath = srcDir @@ projectName @@ "bin" @@ configuration @@ projectName + "." + semanticVersion + ".nupkg"
    let result = Shell.Exec("dotnet", "nuget push " + packagePath + " --source " + nugetSource)

    if result <> 0 then failwithf "Push failed with exit code %d" result
)

Target "All"
    DoNothing

// build order. Pass "-ev push true" to build script to push to NuGet
"Restore"
    ==> "Build"
    ==> "Test"
    =?> ("Push", getEnvironmentVarAsBool "push")
    ==> "All"

RunTargetOrDefault "All"