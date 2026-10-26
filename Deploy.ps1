
$ErrorActionPreference = 'Stop'

# Project configuration
$ProjectPath = ".\WClocks\WClocks.csproj"
$Configuration = "Release"

# Build output directory
$ProjectDir = Split-Path -Parent $ProjectPath
$BuildOutputDir = Join-Path $ProjectDir "bin\$Configuration"

# Installation paths
$InstallRoot = Join-Path $env:LOCALAPPDATA "WClocks"
$AppDir = Join-Path $InstallRoot "App"
$ExePath = Join-Path $AppDir "WClocks.exe"

# Resolve MSBuild
$MSBuildPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $MSBuildPath)) {
    $VsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

    if (Test-Path $VsWhere) {
        $VSPath = & $VsWhere -latest -products '*' `
            -requires Microsoft.Component.MSBuild `
            -property installationPath

        if ($VSPath) {
            $MSBuildPath = Join-Path $VSPath `
                "MSBuild\Current\Bin\MSBuild.exe"
        }
    }
}

try {
    if (-not (Test-Path $ProjectPath)) {
        throw "Project file not found: $ProjectPath"
    }

    if (-not (Test-Path $MSBuildPath)) {
        throw "MSBuild.exe not found. Install Visual Studio Build Tools."
    }

    # 1. Build the Release configuration
    Write-Host "Building Release configuration..." -ForegroundColor Cyan

    & $MSBuildPath $ProjectPath `
    /t:Rebuild `
    "/p:Configuration=$Configuration" `
    /m `
    /nologo

if ($LASTEXITCODE -ne 0) {
    throw "Build failed. The running application was not stopped."
}

    $BuildExe = Join-Path $BuildOutputDir "WClocks.exe"

    if (-not (Test-Path $BuildExe)) {
        throw "Build output not found: $BuildExe"
    }

    # 2. Stop running application instances
    Write-Host "Stopping WClocks..." -ForegroundColor Cyan

    $Processes = @(Get-Process -Name "WClocks" `
        -ErrorAction SilentlyContinue)

    foreach ($Process in $Processes) {
        if ($Process.CloseMainWindow()) {
            $Process.WaitForExit(10000) | Out-Null
        }

        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id -Force
            $Process.WaitForExit()
        }
    }

    # 3. Replace the installed application
    Write-Host "Installing application..." -ForegroundColor Cyan

    New-Item -ItemType Directory -Path $InstallRoot -Force |
        Out-Null

    if (Test-Path $AppDir) {
        Remove-Item -Path $AppDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $AppDir -Force |
        Out-Null

    Copy-Item -Path (Join-Path $BuildOutputDir "*") `
        -Destination $AppDir -Recurse -Force

    if (-not (Test-Path $ExePath)) {
        throw "Installed executable not found: $ExePath"
    }

    # 4. Start the installed application
    Write-Host "Starting WClocks..." -ForegroundColor Cyan

    Start-Process -FilePath $ExePath -WorkingDirectory $AppDir

    Write-Host "Deployment completed successfully." -ForegroundColor Green
    Write-Host "Application path: $ExePath"
}
catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    exit 1
}
