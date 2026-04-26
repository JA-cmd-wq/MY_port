$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = 'E:\DevTools\dotnet-home'
$env:NUGET_PACKAGES = 'E:\NuGetPackages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
& 'E:\DevTools\dotnet\dotnet.exe' build "$PSScriptRoot\..\MY_port.sln" --configuration Release
