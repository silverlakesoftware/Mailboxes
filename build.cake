// Arguments

var target = Argument("target", "Default");

var configuration = Argument("configuration", "Release");

// Tasks

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
         ArgumentCustomization = args=>args.Append($"--logger trx;LogFileName=TestResults.trx")
      };
      DotNetCoreTest(".", settings);
   }
   catch
   {       
       throw;
   }
   finally
   {
      TFBuild.Commands.PublishTestResults(
      new TFBuildPublishTestResultsData {
         TestResultsFiles = GetFiles("**/TestResults.trx").ToArray(),
         TestRunner = TFTestRunnerType.VSTest
      });
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

Task("Default")
.IsDependentOn("Restore")
.IsDependentOn("Build")
.IsDependentOn("Test")
.IsDependentOn("Pack");

RunTarget(target);