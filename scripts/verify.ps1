$ErrorActionPreference = "Stop"

cd z:\repos\chronicis\

dotnet --version
dotnet restore
dotnet build
dotnet test