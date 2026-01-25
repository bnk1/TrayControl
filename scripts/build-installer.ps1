param(
    [string]$SolutionRoot = ".",
    [string]$AppProj = ".\CompactAppWinForms\CompactAppWinForms.csproj",
    [string]$InstallerProj = ".\Installer\CompactApp.wixproj",
    [string]$Configuration = "Release"
)

Write-Host "Build Installer Script" -ForegroundColor Cyan
Write-Host "SolutionRoot: $SolutionRoot"
Write-Host "AppProj: $AppProj"
Write-Host "InstallerProj: $InstallerProj"
Write-Host "Configuration: $Configuration"
Write-Host ""

# Optional: ensure dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI not found in PATH. Install .NET SDK (8.0+) or update PATH."
    exit 1
}

Push-Location $SolutionRoot
try {
    Write-Host "Restoring NuGet packages..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }

    Write-Host "Building application project..."
    dotnet build $AppProj -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "dotnet build for app project failed" }

    Write-Host "Building installer project..."
    dotnet build $InstallerProj -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "dotnet build for installer project failed" }

    # Find the produced MSI
    $installerDir = Split-Path -Parent $InstallerProj
    $searchPath = Join-Path $installerDir "bin\$Configuration"
    Write-Host "Searching for MSI in: $searchPath"
    $msi = Get-ChildItem -Path $searchPath -Filter *.msi -Recurse -ErrorAction SilentlyContinue |
           Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($null -eq $msi) {
        Write-Warning "No MSI found under $searchPath. Check build output or project OutputName."
        exit 1
    }

    Write-Host ""
    Write-Host "MSI built successfully:" -ForegroundColor Green
    Write-Host $msi.FullName
    exit 0
}
catch {
    Write-Error $_
    exit 1
}
finally {
    Pop-Location
}
