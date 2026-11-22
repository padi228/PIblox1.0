$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Publishing PIblox for win-x64..."
dotnet publish "src/PIblox.App/PIblox.App.csproj" --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "artifacts/publish"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$isccCommand = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
$iscc = if ($isccCommand) { $isccCommand.Source } else { $null }
if (-not $iscc) {
    $programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $candidate = Join-Path $programFilesX86 "Inno Setup 6\ISCC.exe"
    if (Test-Path $candidate) { $iscc = $candidate }
}
if (-not $iscc) {
    throw "PIblox portable build is ready in artifacts\publish, but Inno Setup 6 is required to create PIblox_Setup.exe. Install Inno Setup and rerun this script."
}

Write-Host "Building PIblox_Setup.exe..."
& $iscc "/Qp" "installer\PIblox.iss"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE" }

Write-Host "Done: artifacts\PIblox_Setup.exe"
