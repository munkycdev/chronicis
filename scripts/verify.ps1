$ErrorActionPreference = "Stop"

cd z:\repos\chronicis\

dotnet --version
dotnet format .\Chronicis.CI.sln
dotnet restore .\Chronicis.CI.sln
dotnet build .\Chronicis.CI.sln
dotnet test .\Chronicis.CI.sln
