#I "Src/packages/FAKE/tools"
#r "FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open Fake.EnvironmentHelper
open Fake.MSBuildHelper
open Fake.NuGetHelper
open Fake.Testing.XUnit2

// properties
let projectName = "Genesis.RetryWithBackoff"
let semanticVersion = "1.0.2-alpha"
let version = (>=>) @"(?<major>\d*)\.(?<minor>\d*)\.(?<build>\d*).*?" "${major}.${minor}.${build}.0" semanticVersion
let configuration = getBuildParamOrDefault "configuration" "Release"
// can be set by passing: -ev deployToNuGet true
let deployToNuGet = getBuildParamOrDefault "deployToNuGet" "false"
let genDir = "Gen/"
let srcDir = "Src/"
let packagesDir = "Src/packages/"
let testDir = genDir @@ "Test"
let nugetDir = genDir @@ "NuGet"

Target "Clean" (fun _ ->
    CleanDirs[genDir; testDir; nugetDir]

    build (fun p ->
        { p with
            Verbosity = Some(Quiet)
            Targets = ["Clean"]
            Properties = ["Configuration", configuration]
        })
        (srcDir @@ projectName + ".sln")
)

// would prefer to use the built-in RestorePackages function, but it restores packages in the root dir (not in Src), which causes build problems
Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (
        RestorePackage (fun p ->
            { p with
                OutputPath = (srcDir @@ "packages")
            })
        )
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

    build (fun p ->
        { p with
            Verbosity = Some(Quiet)
            Targets = ["Build"]
            Properties =
                [
                    "Optimize", "True"
                    "DebugSymbols", "True"
                    "Configuration", configuration
                ]
        })
        (srcDir @@ projectName + ".sln")
)

Target "ExecuteUnitTests" (fun _ ->
    xUnit2 (fun p ->
        { p with
            ShadowCopy = false;
        })
        [srcDir @@ projectName + ".UnitTests/bin" @@ configuration @@ projectName + ".UnitTests.dll"]
)

Target "CreateArchives" (fun _ ->
    // source archive
    !! "**/*.*"
        -- ".git/**"
        -- (genDir @@ "**")
        -- (srcDir @@ "**/.vs/**")
        -- (srcDir @@ "packages/**/*")
        -- (srcDir @@ "**/*.suo")
        -- (srcDir @@ "**/*.csproj.user")
        -- (srcDir @@ "**/*.gpState")
        -- (srcDir @@ "**/bin/**")
        -- (srcDir @@ "**/obj/**")
        |> Zip "." (genDir @@ projectName + "-" + semanticVersion + "-src.zip")

    // binary archive
    let workingDir = srcDir @@ projectName + "/bin" @@ configuration

    !! (workingDir @@ projectName + ".*")
        |> Zip workingDir (genDir @@ projectName + "-" + semanticVersion + "-bin.zip")
)

Target "CreateNuGetPackages" (fun _ ->
    // copy files required in the NuGet
    !! (srcDir @@ projectName + "/bin" @@ configuration @@ projectName + ".*")
        |> CopyFiles (nugetDir @@ projectName + "/lib/portable-net45+win8+wp8+wpa81")

    // copy source
    let sourceFiles =
        [!! (srcDir @@ "**/*.*")
            -- ".git/**"
            -- (srcDir @@ "**/.vs/**")
            -- (srcDir @@ "packages/**/*")
            -- (srcDir @@ "**/*.suo")
            -- (srcDir @@ "**/*.csproj.user")
            -- (srcDir @@ "**/*.gpState")
            -- (srcDir @@ "**/bin/**")
            -- (srcDir @@ "**/obj/**")]
    sourceFiles
        |> CopyWithSubfoldersTo (nugetDir @@ projectName)

    // create the NuGets
    NuGet (fun p ->
        {p with
            Project = projectName
            Version = semanticVersion
            OutputPath = nugetDir
            WorkingDir = nugetDir @@ projectName
            SymbolPackage = NugetSymbolPackage.Nuspec
            Publish = System.Convert.ToBoolean(deployToNuGet)
            Dependencies =
                [
                    "Rx-Main", GetPackageVersion packagesDir "Rx-Main"
                ]
        })
        (srcDir @@ projectName + ".nuspec")
)

// build order
"Clean"
    ==> "RestorePackages"
    ==> "Build"
    ==> "ExecuteUnitTests"
    ==> "CreateArchives"
    ==> "CreateNuGetPackages"

RunTargetOrDefault "CreateNuGetPackages"