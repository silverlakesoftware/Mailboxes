#tool "nuget:?package=NuGet.CommandLine&version=5.3.1"

// Arguments

var target = Argument("target", "Default");

var configuration = Argument("configuration", "Release");

var nugetPushKey = EnvironmentVariable("NugetPushKey");

var nugetPushSource = EnvironmentVariable("NugetPushSource");

// Tasks

Setup(_ => {
   var cmd = BuildSystem.IsRunningOnAzurePipelinesHosted  ? "cloud" : "get-version";
   DotNetCoreTool(".", "nbgv", cmd);
});

Task("Clean")
.Does(() => {   
   DeleteDirectories(new [] {
      new DirectoryPath("./temp"), 
      new DirectoryPath("./output")},
      new DeleteDirectorySettings {
         Recursive = true,
         Force = true
      });
});

Task("Restore")
.Does(() => {
   DotNetCoreRestore(".");
});

Task("Build")
.Does(() => {
   var settings = new DotNetCoreBuildSettings 
   {
      NoRestore = true,
      Configuration = configuration
   };
   DotNetCoreBuild(".", settings);
});

Task("Test")
.Does(() => {

   try
   {
      var settings = new DotNetCoreTestSettings
      {
         NoBuild = true,
         Configuration = configuration,
         Filter = "Category!=TimingSensitive",
      };
      if (BuildSystem.IsRunningOnAzurePipelinesHosted)
      {
         settings.ArgumentCustomization = args => args.Append($"--logger trx;LogFileName=TestResults.trx");
      }
      DotNetCoreTest(".", settings);
   }
   catch
   {       
       throw;
   }
   finally
   {
      if (BuildSystem.IsRunningOnAzurePipelinesHosted)
      {
         TFBuild.Commands.PublishTestResults(
         new TFBuildPublishTestResultsData {
            TestResultsFiles = GetFiles("**/TestResults.trx").ToArray(),
            TestRunner = TFTestRunnerType.VSTest
         });
      }
   }
});

Task("Pack")
.Does(() => {
   var settings = new DotNetCorePackSettings
   {
      NoBuild = true,
      Configuration = configuration
   };
   DotNetCorePack(".", settings);
});

Task("Publish")
.Does(() => {

   if (nugetPushKey==null || nugetPushSource==null)
   {
      Error("Unable to deploy: NugetPushKey or NugetPushSource is not defined.");
   }

   foreach(var file in GetFiles(@$"output\{configuration}\*.nupkg"))
   {
      NuGetPush(file,
      new NuGetPushSettings
      {
         Source = nugetPushSource,
         ApiKey = nugetPushKey,
      });
   }
});

Task("Default")
.IsDependentOn("Restore")
.IsDependentOn("Build")
.IsDependentOn("Test")
.IsDependentOn("Pack");

RunTarget(target);