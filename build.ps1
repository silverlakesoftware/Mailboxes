[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

# Restore Cake tool
& dotnet tool restore

# Build Cake arguments
$cakeArguments = @("$Script");
if ($Target) { $cakeArguments += "--target=$Target" }
if ($Verbosity) { $cakeArguments += "-verbosity=$Verbosity" }
$cakeArguments += $ScriptArgs

& dotnet tool run dotnet-cake -- $cakeArguments
exit $LASTEXITCODE