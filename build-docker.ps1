<#
.SYNOPSIS
    Modular PowerShell Build Script

.DESCRIPTION
    This script performs version file generation, MSBuild or Sonar-based build,
    optional interop assembly generation, and optional type library generation.

.PARAMETER TeamCityUri
    Overrides the teamcityuri appSetting.
.PARAMETER TeamCityUsername
    Overrides the teamcityusername appSetting.
.PARAMETER TeamCityPassword
    Overrides the teamcitypassword appSetting.
.PARAMETER GitLabUri
    Overrides the gitlaburi appSetting.
.PARAMETER GitLabUsername
    Overrides the gitlabusername appSetting.
.PARAMETER GitLabPassword
    Overrides the gitlabpassword appSetting.

.ENVIRONMENT VARIABLES
    $env:initial_year               Base year for version calculation
    $env:build_counter              Counter for versioning
    $env:major_ver                  Major version
    $env:minor_ver                  Minor version
    $env:build_vcs_number           VCS commit or build number
    $env:build_config_id            CI build configuration ID
    $env:projectBranch              Git branch name
    $env:SONARQUBE_SERVER_URL       SonarQube server URL
    $env:SONARQUBE_SERVER_TOKEN     SonarQube authentication token
    $env:perform_codescan           'true' to enable code scan
    $env:skip_grunt                 'true' to skip the Grunt front-end asset build
                                    even when a Grunt setup is present
    $env:skip_webconfig_transform   'true' to skip the web.config transform even
                                    when a WebConfigTransform.proj is present
    $env:skip_tests                 'true' to skip the unit test run entirely
    $env:test_skip_patterns         Comma/semicolon-separated .NET regexes matched
                                    (case-insensitively) against test assembly names;
                                    matches are not run. Overrides the in-script
                                    $testSkipPatterns default.
    $env:solution_name              Base name (no extension) of the .sln to build,
                                    when src\ contains more than one. Auto-detected
                                    (single .sln under src\) when unset.
    $env:project_name               Overrides the derived project name used for the
                                    Sonar project key and version paths. Defaults to
                                    the solution's base name.
    $env:publish_project            Project(s) to `dotnet publish` as the artifact: a
                                    project name or .csproj path, or a comma/semicolon-
                                    separated list of them. A single project publishes flat
                                    into the artifact folder; a list publishes each into its
                                    own build\<ProjectName>\ subfolder (so independent
                                    deliverables can't clobber each other's dependencies).
                                    When unset, auto-detected: the single non-test Exe/WinExe
                                    or Microsoft.NET.Sdk.Web project, else (a library-only
                                    solution) the single non-test library "root". Set this
                                    when detection is ambiguous (e.g. several independent
                                    deliverable libraries) or the artifact is missing files
                                    or has extra ones.
    $env:publish_subfolders         'true' to publish EACH project into its own
                                    build\<ProjectName>\ subfolder, even when there is only
                                    one. Default: a single project publishes flat and only
                                    multiple projects are foldered per project. (Same effect
                                    as the $publishInProjectSubfolders toggle near the top.)
    $env:publish_all_projects       'true' to publish EVERY non-test project, each into its
                                    own build\<ProjectName>\ subfolder, instead of just the
                                    auto-detected deliverable/root. For repos that ship
                                    several independent libraries. Test projects excluded.
    $env:company_name               AssemblyCompany value. Defaults in-script.
    $env:product_name               AssemblyProduct value. Defaults in-script.
    $env:dotnet_scan_mode           Optional override for the managed (C#) scanner
                                    begin/end driver: 'dotnet' (dotnet-sonarscanner
                                    global tool) or 'framework' (SonarScanner.MSBuild.exe).
                                    Auto-detected from what's installed when unset.
                                    The build tool (dotnet build vs msbuild) is chosen
                                    separately from the project style.
#>

param (
    [string] $TeamCityUri,
    [string] $TeamCityUsername,
    [string] $TeamCityPassword,
    [string] $GitLabUri,
    [string] $GitLabUsername,
    [string] $GitLabPassword
)

# Load Environment Variables
$initial_year = $env:initial_year
$build_counter = $env:build_counter
$major_ver = $env:major_ver
$minor_ver = $env:minor_ver
$build_vcs_number = if ([string]::IsNullOrEmpty($env:build_vcs_number)) { "LOCAL" } else { $env:build_vcs_number }
$build_config_id = $env:build_config_id
$projectBranch = $env:projectBranch
$sonarQubeServerUrl = $env:SONARQUBE_SERVER_URL
$sonarQubeToken = $env:SONARQUBE_SERVER_TOKEN
$performCodeScan = ($env:perform_codescan -match 'true' -and -not [string]::IsNullOrEmpty($sonarQubeServerUrl))


# Constants and Derived Values
# $project (solution / main project name) is resolved from the single .sln under
# src\ in the Paths section below, so this script is reusable across repos.
$performNuGetRestore = $true
$generateTypeLibrary = $false
$generateInteropAssembly = $false
# Publish EACH deployable project into its own build\<ProjectName>\ subfolder, even when
# there is only one. Default $false: a single project publishes flat (the artifact path
# every repo/deploy relies on) and only multiple projects are foldered per project. Flip
# this to $true here, or set env:publish_subfolders='true' per build without editing this.
$publishInProjectSubfolders = ($env:publish_subfolders -match 'true')
# Publish EVERY non-test project (each into its own build\<ProjectName>\ subfolder) instead
# of just the auto-detected deliverable/root. Use when a repo ships several libraries
# independently rather than one root that references the others; note a dependency also
# gets its own folder AND is copied into each dependent's folder. Test projects are always
# excluded. Flip to $true here, or set env:publish_all_projects='true' per build.
$publishAllProjects = ($env:publish_all_projects -match 'true')
$companyName = if ($env:company_name) { $env:company_name } else { "Green Shield Administration Inc." }
$productName = if ($env:product_name) { $env:product_name } else { "VITAL Objects" }
$currentYear = (Get-Date).Year
$date = [DateTime]::Now
$global:config = "release"
$xunitVersion = "2.9.3"

# Test assemblies whose name matches ANY of these regexes are not run. Matched
# case-insensitively against the assembly name without extension (e.g.
# "VOAPI.IntegrationTests"). Integration tests need external dependencies
# (databases, queues, services) that the build agent doesn't have, so they are
# excluded from the CI unit test run. $env:test_skip_patterns overrides this list.
$testSkipPatterns = @(
    'IntegrationTests?$'   # VOAPI.IntegrationTests
)

# Versioning
$buildVer = [string]::Format("{0}{1:00}{2:00}", $date.Year - $initial_year, $date.Month, $date.Day)
Write-Host "Build Ver: $buildVer"

$revisionVer = if ($build_counter) { $build_counter } else { "0" }
$voVersion = "$major_ver.$minor_ver.0.0"
Write-Host "VO Ver: $voVersion"

$fileVersion = "$major_ver.$minor_ver.$buildVer.$revisionVer"
Write-Host "File Ver: $fileVersion"

$assemblyVersion = $voVersion
$copyrightInfo = "Copyright (C) $currentYear $companyName"

# Paths
$base_dir = Resolve-Path .
$build_dir = "$base_dir\build"
$source_dir = "$base_dir\src"
$tools_dir = "$base_dir\tools"
$dependency_report_dir = "$base_dir\report"
$commonAssemblyInfoFileName = "$source_dir\CommonAssemblyInfo.cs"
$buildPropsFileName = "$source_dir\Directory.Build.props"
$consoleConfigFileName = "$source_dir\TeamCityConsole\App.config"

$applicationSettingOverrides = [ordered]@{}
if ($PSBoundParameters.ContainsKey("TeamCityUri")) {
    $applicationSettingOverrides["teamcityuri"] = $TeamCityUri
}
if ($PSBoundParameters.ContainsKey("TeamCityUsername")) {
    $applicationSettingOverrides["teamcityusername"] = $TeamCityUsername
}
if ($PSBoundParameters.ContainsKey("TeamCityPassword")) {
    $applicationSettingOverrides["teamcitypassword"] = $TeamCityPassword
}
if ($PSBoundParameters.ContainsKey("GitLabUri")) {
    $applicationSettingOverrides["gitlaburi"] = $GitLabUri
}
if ($PSBoundParameters.ContainsKey("GitLabUsername")) {
    $applicationSettingOverrides["gitlabusername"] = $GitLabUsername
}
if ($PSBoundParameters.ContainsKey("GitLabPassword")) {
    $applicationSettingOverrides["gitlabpassword"] = $GitLabPassword
}

function Set-ApplicationSettings {
    if ($applicationSettingOverrides.Count -eq 0) {
        return
    }

    if (-not (Test-Path -LiteralPath $consoleConfigFileName)) {
        Write-Host "##teamcity[message text='Application config not found: $consoleConfigFileName' status='ERROR']"
        exit 1
    }

    [xml]$xml = Get-Content -LiteralPath $consoleConfigFileName -Raw
    $settings = @($xml.configuration.appSettings.add)

    foreach ($override in $applicationSettingOverrides.GetEnumerator()) {
        $setting = $settings |
            Where-Object { $_.key -eq $override.Key } |
            Select-Object -First 1

        if (-not $setting) {
            Write-Host "##teamcity[message text='Application config setting not found: $($override.Key)' status='ERROR']"
            exit 1
        }

        $setting.value = [string]$override.Value
    }

    $xml.Save($consoleConfigFileName)
    Write-Host "Updated application settings in $consoleConfigFileName"
}

# Solution + project name. The scaffolding tool substitutes TeamCityApi with the
# repo's solution / main-project base name when this template is instantiated. If the
# placeholder is left unsubstituted, the values are auto-detected from the single
# solution under src\ instead. Env vars ($env:solution_name / $env:project_name) still
# override either source. The solution extension is auto-selected: .sln is used when
# present, otherwise .slnx, so repos on either format build unchanged.
$projectNameToken = "TeamCityApi"
$projectNameIsSet = $projectNameToken -notmatch '\{\{.*\}\}'   # false while still the raw placeholder

# Resolves the solution file, auto-selecting the extension (prefer .sln, then .slnx).
# With a base name it matches <name>.sln / <name>.slnx under src\; without one it
# auto-detects the single solution (a repo carrying both formats of the same name is
# treated as one solution and .sln wins).
function Resolve-SolutionPath([string]$baseName) {
    if ($baseName) {
        $sln  = Join-Path $source_dir "$baseName.sln"
        $slnx = Join-Path $source_dir "$baseName.slnx"
        if (Test-Path $sln)  { return $sln }
        if (Test-Path $slnx) { return $slnx }
        return $sln   # neither present — keep the .sln path so a later "not found" reads naturally
    }

    $solutions = @(Get-ChildItem -Path $source_dir -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -eq '.sln' -or $_.Extension -eq '.slnx' })

    if ($solutions.Count -eq 0) {
        Write-Host "##teamcity[message text='No .sln or .slnx found under $source_dir' status='ERROR']"; exit 1
    }

    $distinctNames = @($solutions | Select-Object -ExpandProperty BaseName -Unique)
    if ($distinctNames.Count -gt 1) {
        Write-Host "##teamcity[message text='Multiple solutions under $source_dir; set env:solution_name to choose' status='ERROR']"; exit 1
    }

    # One solution (possibly present as both .sln and .slnx) — prefer .sln.
    $preferred = $solutions | Where-Object { $_.Extension -eq '.sln' } | Select-Object -First 1
    if ($preferred) { return $preferred.FullName }
    return ($solutions | Select-Object -First 1).FullName
}

$solutionPath =
    if ($env:solution_name)    { Resolve-SolutionPath $env:solution_name }
    elseif ($projectNameIsSet) { Resolve-SolutionPath $projectNameToken }
    else                       { Resolve-SolutionPath '' }
$project =
    if ($env:project_name) { $env:project_name }
    elseif ($projectNameIsSet) { $projectNameToken }
    else { [System.IO.Path]::GetFileNameWithoutExtension($solutionPath) }
Write-Host "Solution: $solutionPath"
Write-Host "Project:  $project"

$versionIncPath = "$source_dir\$project\version.inc"
$buildDllPath = "$build_dir\$project.dll"

# The MSBuild used for the solution build. Resolved at runtime by Resolve-MSBuild
# (see Functions) once the solution path is known, because the required bitness
# depends on whether the solution contains a Web Site project.
$msbuild = "msbuild"

# Files/patterns excluded from SonarQube analysis. The build script itself
# (*.ps1), build output and VCS metadata are not product code; the remaining
# patterns are C/C++ generated/IDE artifacts kept for parity with the native
# scan. NOTE: a scanner-supplied sonar.exclusions REPLACES (does not merge with)
# any list configured on the SonarQube server, so the full list lives here.
$sonarExclusions = @(
    '**/*.ps1'        # build scripts
    '**/build/**'     # compiled output
    '**/.git/**'      # VCS metadata
    '**/.vs/**'       # Visual Studio cache
    '**/Release/**'
    '**/Debug/**'
    '**/*.inc'
    '**/*_i.h'
    '**/*_i.c'
    '**/*_p.c'
    '**/*.tlh'
    '**/*.tli'
    '**/packages/**'
    '**/assemblies/**'
) -join ','

# Functions

function Create-VersionFile {
    param (
        [string]$major,
        [string]$minor,
        [string]$build,
        [string]$revision,
        [string]$filename,
        [string]$projectName
    )
    @"
// This file gets overwritten during Automated Builds

#define APP_F_MAJORNUMBER $major
#define APP_F_MINORNUMBER $minor
#define APP_F_BUILDNUMBER $build
#define APP_F_MODIFICATIONNUMBER $revision
#define APP_F_BUILD_VCS_NUMBER $build_vcs_number
#define APP_F_FILEDESCRIPTION "$projectName - $(($build_vcs_number.PadRight(10,"*")).Substring(0,6))"

#define APP_P_MAJORNUMBER $major
#define APP_P_MINORNUMBER $minor
#define APP_P_BUILDNUMBER $build
#define APP_P_MODIFICATIONNUMBER $revision
#define APP_P_BUILD_VCS_NUMBER $build_vcs_number

#define COPYRIGHT_YEAR $currentYear
"@ | Out-File $filename -Encoding ASCII
}

function Clear-BuildFolder {
    if (-not (Test-Path $build_dir)) { return }
    Write-Host "Removing contents of $build_dir"

    # Release handles held by the .NET build servers (VBCSCompiler / MSBuild). They
    # persist after a build even with /nodeReuse:false and keep freshly-built output
    # DLLs open — the usual cause of UnauthorizedAccessException on the delete below.
    # (The read-only strip alone did not fix it: the files are locked, not just
    # read-only.)
    try { dotnet build-server shutdown 2>$null | Out-Null } catch { }

    # Assemblies copied from read-only sources (the NuGet cache, src\assemblies) also
    # keep the read-only attribute, which a bind-mount Remove-Item -Force won't clear.
    Get-ChildItem -Path $build_dir -Recurse -Force -File -ErrorAction SilentlyContinue |
        ForEach-Object { try { $_.IsReadOnly = $false } catch { } }

    # Even so, a just-written DLL can be briefly locked (a lingering compiler server, or
    # the container's antivirus scanning the new file). Retry with a short delay, and
    # remove files AND directories (aspnet_compiler refuses a non-empty target, so a
    # leftover empty dir like build\_PublishedWebsites\ICS would fail a later build).
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        Get-ChildItem -Path $build_dir -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        $remaining = @(Get-ChildItem -Path $build_dir -Force -ErrorAction SilentlyContinue)
        if ($remaining.Count -eq 0) {
            Write-Host "Clear-BuildFolder: $build_dir cleared successfully (attempt $attempt/5)"
            return
        }
        Write-Host "Clear-BuildFolder: $($remaining.Count) item(s) still locked; retrying in 2s (attempt $attempt/5)"
        Start-Sleep -Seconds 2
    }

    $stuck = @(Get-ChildItem -Path $build_dir -Recurse -Force -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty FullName)
    Write-Host "##teamcity[message text='Could not clear $build_dir; still locked: $($stuck -join '; ')' status='ERROR']"
    exit 1
}

function ConvertTo-TeamCityEscaped {
    # Escape the characters that are significant in TeamCity service messages.
    param (
        [string]$value
    )
    if ($null -eq $value) { return "" }
    return $value -replace '\|', '||' `
                  -replace "'", "|'" `
                  -replace '\[', '|[' `
                  -replace '\]', '|]' `
                  -replace "`r", '|r' `
                  -replace "`n", '|n'
}

function Restore-NuGetPackages {
    param (
        [string]$solutionPath
    )

    if (-not $performNuGetRestore) {
        Write-Host "Skipping NuGet restore (performNuGetRestore = false)"
        return
    }

    # nuget restore at the solution level handles both project styles:
    #   - packages.config projects (classic .NET Framework)
    #   - PackageReference projects (SDK-style / newer)
    # (msbuild /restore and dotnet restore only cover PackageReference.)
    Write-Host "Restoring NuGet packages for $solutionPath"

    # Capture the output (merging stderr) so we can scan it for vulnerability
    # warnings, while still echoing every line to the build log.
    $restoreOutput = nuget restore $solutionPath 2>&1
    $restoreOutput | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { exit 1 }

    # Web Site projects are folder-based (no .csproj), so `nuget restore <solution>`
    # does not discover their packages.config and reports "Nothing to do" — leaving
    # src\packages\ empty and the build's Copy task emitting MSB3030 for the missing
    # Newtonsoft.Json / SharpZipLib DLLs. Restore every packages.config explicitly.
    # -SolutionDirectory anchors the packages folder at <solutionDir>\packages
    # (= src\packages), which is exactly where the build copies the assemblies from.
    $solutionDir = Split-Path -Parent $solutionPath
    $packagesConfigs = @(Get-ChildItem -Path $solutionDir -Recurse -Filter packages.config -ErrorAction SilentlyContinue)
    foreach ($config in $packagesConfigs) {
        Write-Host "Restoring packages for $($config.FullName)"
        nuget restore $config.FullName -SolutionDirectory $solutionDir
        if ($LASTEXITCODE -ne 0) { exit 1 }
    }

    # NuGet audit emits NU1901 (low) .. NU1904 (critical) for vulnerable packages.
    # Surface each unique finding as a TeamCity warning, and tag the build status.
    $vulnerabilities = $restoreOutput |
        Select-String -Pattern 'NU190[1-4]' |
        ForEach-Object { $_.Line.Trim() } |
        Select-Object -Unique

    foreach ($vuln in $vulnerabilities) {
        $escaped = ConvertTo-TeamCityEscaped $vuln
        Write-Host "##teamcity[message text='$escaped' status='WARNING']"
    }

    if ($vulnerabilities) {
        $count = @($vulnerabilities).Count
        Write-Host "##teamcity[buildStatus text='{build.status.text}, $count vulnerable package warning(s)']"

        # Write a report artifact and publish it to TeamCity for download/archival.
        if (-not (Test-Path $dependency_report_dir)) {
            New-Item -ItemType Directory -Path $dependency_report_dir -Force | Out-Null
        }
        $reportPath = Join-Path $dependency_report_dir "vulnerable-packages.txt"

        $reportHeader = @(
            "Vulnerable NuGet packages report"
            "Solution    : $solutionPath"
            "Build       : $fileVersion ($build_vcs_number)"
            "Findings    : $count"
            ("-" * 60)
        )
        ($reportHeader + $vulnerabilities) | Set-Content -Path $reportPath -Encoding UTF8

        Write-Host "Wrote vulnerable package report to $reportPath"
        Write-Host "##teamcity[publishArtifacts '$reportPath']"
    }
}

function Test-SolutionHasWebSite {
    param (
        [string]$solutionPath
    )

    # Web Site projects are folder-based (no .csproj) and carry a
    # WebsiteProperties section in the .sln. That section is what triggers
    # aspnet_compiler at build time, so its presence is the signal that MSBuild
    # bitness matters (see Resolve-MSBuild).
    if (-not (Test-Path $solutionPath)) { return $false }
    return [bool](Select-String -Path $solutionPath -Pattern 'ProjectSection\(WebsiteProperties\)' -Quiet)
}

function Resolve-MSBuild {
    param (
        [string]$solutionPath
    )

    # Chooses which MSBuild builds the solution.
    #
    # A Web Site project is precompiled by aspnet_compiler, which runs with the
    # SAME bitness as the msbuild that spawns it. Legacy web stacks here reference
    # x86 COM/interop assemblies (e.g. CWIBO.AddIns), and a 64-bit aspnet_compiler
    # cannot load them:
    #   ASPNETCOMPILER : error ASPCONFIG: Could not load ... An attempt was made
    #   to load a program with an incorrect format.
    # For those solutions we must use the 32-bit msbuild (which keeps
    # aspnet_compiler under Framework\, not Framework64\).
    #
    # Every other solution uses the default msbuild on PATH (normally the 64-bit
    # engine, with a larger address space — safer for big managed builds and
    # analyzer runs). So this only constrains bitness where it's actually required.
    if (-not (Test-SolutionHasWebSite $solutionPath)) {
        Write-Host "No Web Site project in solution; using default msbuild on PATH"
        return "msbuild"
    }

    Write-Host "Web Site project detected; resolving 32-bit msbuild for aspnet_compiler compatibility"

    # vswhere is the version-agnostic locator: its own path under
    # 'Microsoft Visual Studio\Installer' is stable across VS releases (2019,
    # 2022, 2026, ...), so this keeps working when the VS product directory
    # changes. -find returns BOTH the 32-bit (...\Bin\MSBuild.exe) and 64-bit
    # (...\Bin\amd64\MSBuild.exe) engines; the 32-bit one is simply the path NOT
    # under an 'amd64' folder.
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msbuild32 = & $vswhere -latest -prerelease -products * `
            -requires Microsoft.Component.MSBuild `
            -find "MSBuild\**\Bin\MSBuild.exe" |
            Where-Object { $_ -notmatch '\\amd64\\' } |
            Select-Object -First 1
        if ($msbuild32 -and (Test-Path $msbuild32)) {
            Write-Host "Using 32-bit msbuild: $msbuild32"
            return $msbuild32
        }
    }

    # Fallback: glob any installed VS edition/year under either Program Files
    # location (no version pinned) and take the non-amd64 (32-bit) MSBuild.
    $globs = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\MSBuild.exe"
    )
    $msbuild32 = Get-ChildItem -Path $globs -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\amd64\\' } |
        Select-Object -First 1 -ExpandProperty FullName
    if ($msbuild32) {
        Write-Host "Using 32-bit msbuild (glob fallback): $msbuild32"
        return $msbuild32
    }

    # Last resort: whatever is on PATH. Warn loudly — if this resolves to a 64-bit
    # msbuild the Web Site precompile will fail on x86 references.
    Write-Host "##teamcity[message text='32-bit msbuild not found; Web Site precompile may fail on x86 references' status='WARNING']"
    return "msbuild"
}

function Perform-Build {
    param (
        [string]$solutionPath
    )

    Write-Host "Performing MSBuild"
    & $msbuild /t:Clean /t:Build /p:Configuration=$config /p:OutDir="$build_dir\\" /p:MSBuildWarningsAsMessages=MSB3270 $solutionPath /nodeReuse:false
    if (-not $?) { exit 1 }
}

function Perform-CodeScanAndBuild {
    param (
        [string]$solutionPath,
        [string]$projectName
    )

    Write-Host "Invoking MSBuild with BuildWrapper"
    build-wrapper-win-x86-64 --out-dir bw-output msbuild /t:Clean /t:Build /p:Configuration=$config /p:OutDir="$build_dir\\" $solutionPath /nodeReuse:false
    if (-not $?) { exit 1 }

    Write-Host "Running SonarQube Scanner"
    $scannerArgs = @(
        "--define", "sonar.host.url=$sonarQubeServerUrl"
        "--define", "sonar.token=$sonarQubeToken"
        "--define", "sonar.cfamily.compile-commands=$base_dir/bw-output/compile_commands.json"
        "--define", "sonar.projectName=$projectName"
        "--define", "sonar.projectKey=$projectName"
        "--define", "sonar.branch.name=$projectBranch"
        "--define", "sonar.sources=src"
        "--define", "sonar.exclusions=$sonarExclusions"
        "--define", "sonar.projectVersion=$fileVersion"
        "--define", "sonar.scm.disabled=true"
    )

    # Reuse the image's JDK (17+) instead of letting the CLI download its own JRE.
    if ($env:JAVA_HOME -and (Test-Path "$env:JAVA_HOME\bin\java.exe")) {
        Write-Host "Using pre-installed Java at $env:JAVA_HOME (skipping JRE provisioning)"
        $scannerArgs += "--define", "sonar.scanner.skipJreProvisioning=true"
        $scannerArgs += "--define", "sonar.scanner.javaExePath=$env:JAVA_HOME\bin\java.exe"
    }
    else {
        Write-Host "JAVA_HOME not set or invalid; scanner will provision its own JRE"
    }

    sonar-scanner $scannerArgs
    if (-not $?) { exit 1 }
}

function Resolve-SonarScannerFrontEnd {
    # Chooses the begin/end driver for the managed scan. This is independent of
    # the build tool: the dotnet global tool can wrap a full-msbuild build of a
    # classic project just as the framework exe can.
    #   'dotnet'    -> dotnet sonarscanner  (the dotnet-sonarscanner global tool)
    #   'framework' -> SonarScanner.MSBuild.exe
    #
    # An explicit override always wins, so a build configuration can force one.
    if ($env:dotnet_scan_mode -in @('dotnet', 'framework')) {
        Write-Host "dotnet_scan_mode override = $env:dotnet_scan_mode"
        return $env:dotnet_scan_mode
    }

    # Prefer the dotnet global tool when present (it's what this image installs),
    # otherwise fall back to the .NET Framework exe if it's on PATH.
    $hasDotnetScanner = ($null -ne (Get-Command dotnet -ErrorAction SilentlyContinue)) -and
        ($null -ne (dotnet tool list --global 2>$null | Select-String -SimpleMatch 'dotnet-sonarscanner'))
    if ($hasDotnetScanner) { return 'dotnet' }

    if ($null -ne (Get-Command SonarScanner.MSBuild.exe -ErrorAction SilentlyContinue)) { return 'framework' }

    Write-Host "##teamcity[message text='No SonarScanner for .NET found (neither dotnet-sonarscanner nor SonarScanner.MSBuild.exe)' status='ERROR']"
    exit 1
}

function Test-AllSdkStyleProjects {
    param (
        [string]$searchDir
    )

    # True only when every managed project is SDK-style (declares an SDK on the
    # root <Project Sdk="..."> element). Classic projects use the old
    # xmlns/ToolsVersion form and must be built with full msbuild; dotnet build
    # cannot reliably build them, so a single classic project forces msbuild.
    $projects = @(Get-ChildItem -Path $searchDir -Recurse -Include *.csproj, *.vbproj -ErrorAction SilentlyContinue)
    if ($projects.Count -eq 0) { return $false }

    foreach ($proj in $projects) {
        $head = (Get-Content -Path $proj.FullName -TotalCount 5 -ErrorAction SilentlyContinue) -join "`n"
        if ($head -notmatch '(?i)<Project\s+Sdk\s*=') {
            Write-Host "Classic-style project found ($($proj.Name)); building with msbuild"
            return $false
        }
    }
    return $true
}

function Perform-CodeScanAndBuild-DotNet {
    param (
        [string]$solutionPath,
        [string]$projectName
    )

    # C# / VB.NET is analysed by the SonarScanner for .NET (the MSBuild
    # integration), NOT by build-wrapper. The Roslyn-based analyzers run as part
    # of the build between the begin and end steps; build-wrapper would only emit
    # an empty compile_commands.json for a managed build.
    #
    # Two independent choices:
    #   * front-end (begin/end): whichever scanner is installed.
    #   * build tool: dotnet build only when ALL projects are SDK-style; otherwise msbuild.
    $frontEnd = Resolve-SonarScannerFrontEnd
    $useDotnetBuild = Test-AllSdkStyleProjects -searchDir $source_dir
    Write-Host "SonarScanner front-end: $frontEnd; build tool: $(if ($useDotnetBuild) { 'dotnet build' } else { 'msbuild' })"

    # The begin switches are identical for both scanner front-ends.
    $beginArgs = @(
        "/k:$projectName"
        "/n:$projectName"
        "/v:$fileVersion"
        "/d:sonar.host.url=$sonarQubeServerUrl"
        "/d:sonar.token=$sonarQubeToken"
        "/d:sonar.branch.name=$projectBranch"
        "/d:sonar.scm.disabled=true"
        "/d:sonar.exclusions=$sonarExclusions"
        # Scope analysis to the source tree so the build script, build output and
        # VCS metadata at the mount root are not indexed.
        "/d:sonar.projectBaseDir=$source_dir"
    )

    # Reuse a pre-installed JDK (17+) from the image so the scanner doesn't
    # download its own JRE on every run. Falls back to the scanner's JRE
    # provisioning when no suitable JAVA_HOME is present.
    if ($env:JAVA_HOME -and (Test-Path "$env:JAVA_HOME\bin\java.exe")) {
        Write-Host "Using pre-installed Java at $env:JAVA_HOME (skipping JRE provisioning)"
        $beginArgs += "/d:sonar.scanner.skipJreProvisioning=true"
        $beginArgs += "/d:sonar.scanner.javaExePath=$env:JAVA_HOME\bin\java.exe"
    }
    else {
        Write-Host "JAVA_HOME not set or invalid; scanner will provision its own JRE"
    }

    # --- begin ---
    Write-Host "Beginning SonarScanner ($frontEnd)"
    if ($frontEnd -eq 'dotnet') {
        dotnet sonarscanner begin $beginArgs
    }
    else {
        SonarScanner.MSBuild.exe begin $beginArgs
    }
    if (-not $?) { exit 1 }

    # --- build (analyzers run here) ---
    # Force a full build so every project is compiled while the analyzers are
    # active; an incremental build can skip projects and yield empty results.
    if ($useDotnetBuild) {
        Write-Host "Building with dotnet build"
        dotnet build $solutionPath -c $config -o "$build_dir" --no-incremental
    }
    else {
        Write-Host "Performing MSBuild (analysis build)"
        & $msbuild /t:Rebuild /p:Configuration=$config /p:OutDir="$build_dir\\" /p:MSBuildWarningsAsMessages=MSB3270 $solutionPath /nodeReuse:false
    }
    if (-not $?) { exit 1 }

    # --- end ---
    Write-Host "Ending SonarScanner ($frontEnd)"
    if ($frontEnd -eq 'dotnet') {
        dotnet sonarscanner end /d:sonar.token="$sonarQubeToken"
    }
    else {
        SonarScanner.MSBuild.exe end /d:sonar.token="$sonarQubeToken"
    }
    if (-not $?) { exit 1 }
}

function Perform-CodeScan-WebSite {
    param (
        [string]$solutionPath,
        [string]$projectName
    )

    # ASP.NET Web Site projects have no .csproj/ProjectGuid, so the SonarScanner
    # for .NET (begin/build/end MSBuild integration) has nothing to hook into and
    # its end step fails with "unable to collect the required information about
    # your projects". Instead we build normally (aspnet_compiler, so tests still
    # have output) and analyse the raw sources with the standalone sonar-scanner
    # CLI. The C# analyzer runs source-only here (no Roslyn build context), which
    # is the only mode available for a folder-based Web Site.
    Write-Host "Building Web Site (msbuild) before standalone scan"
    & $msbuild /t:Rebuild /p:Configuration=$config /p:OutDir="$build_dir\\" /p:MSBuildWarningsAsMessages=MSB3270 $solutionPath /nodeReuse:false
    if (-not $?) { exit 1 }

    Write-Host "Running SonarQube Scanner (standalone CLI) for Web Site project"
    $scannerArgs = @(
        "--define", "sonar.host.url=$sonarQubeServerUrl"
        "--define", "sonar.token=$sonarQubeToken"
        "--define", "sonar.projectName=$projectName"
        "--define", "sonar.projectKey=$projectName"
        "--define", "sonar.branch.name=$projectBranch"
        # Scope analysis to the source tree so the build script, build output and
        # VCS metadata at the mount root are not indexed.
        "--define", "sonar.projectBaseDir=$source_dir"
        "--define", "sonar.sources=."
        "--define", "sonar.exclusions=$sonarExclusions"
        "--define", "sonar.projectVersion=$fileVersion"
        "--define", "sonar.scm.disabled=true"
    )

    # Reuse the image's JDK (17+) instead of letting the CLI download its own JRE.
    if ($env:JAVA_HOME -and (Test-Path "$env:JAVA_HOME\bin\java.exe")) {
        Write-Host "Using pre-installed Java at $env:JAVA_HOME (skipping JRE provisioning)"
        $scannerArgs += "--define", "sonar.scanner.skipJreProvisioning=true"
        $scannerArgs += "--define", "sonar.scanner.javaExePath=$env:JAVA_HOME\bin\java.exe"
    }
    else {
        Write-Host "JAVA_HOME not set or invalid; scanner will provision its own JRE"
    }

    sonar-scanner $scannerArgs
    if (-not $?) { exit 1 }
}

function Resolve-NodeTooling {
    # Returns @{ Node=<path>; Npm=<path> } for the Grunt step. Prefers node/npm on
    # PATH (the build image installs Node), and falls back to the repo-bundled
    # tools\nodejs that the legacy TeamCity agents shipped. Returns $null if neither
    # is available.
    $node = (Get-Command node -ErrorAction SilentlyContinue).Source
    $npm = (Get-Command npm -ErrorAction SilentlyContinue).Source
    if ($node -and $npm) {
        Write-Host "Using Node from PATH: $node"
        return @{ Node = $node; Npm = $npm }
    }

    $bundledNode = "$tools_dir\nodejs\node.exe"
    $bundledNpm = "$tools_dir\nodejs\npm.cmd"
    if ((Test-Path $bundledNode) -and (Test-Path $bundledNpm)) {
        Write-Host "Using repo-bundled Node: $bundledNode"
        return @{ Node = $bundledNode; Npm = $bundledNpm }
    }

    return $null
}

function Report-NpmVulnerabilities {
    param (
        [string]$npm,
        [string]$workingDir
    )

    # Surfaces npm dependency vulnerabilities to TeamCity, mirroring the NuGet audit
    # handling in Restore-NuGetPackages (per-finding warnings, a build-status tag and
    # a published report artifact). Non-fatal by design: vulnerable transitive npm
    # packages should flag the build, not fail it.
    #
    # `npm audit --json` is the structured source of truth (the install summary line
    # is not machine-readable). It EXITS NON-ZERO when vulnerabilities exist, so we
    # capture output and deliberately ignore the exit code.
    Push-Location $workingDir
    try {
        $auditJson = & $npm audit --json 2>$null | Out-String
    }
    finally {
        Pop-Location
    }

    if ([string]::IsNullOrWhiteSpace($auditJson)) {
        Write-Host "npm audit produced no output; skipping vulnerability report"
        return
    }

    try {
        $audit = $auditJson | ConvertFrom-Json
    }
    catch {
        Write-Host "Could not parse npm audit output; skipping vulnerability report"
        return
    }

    $meta = $audit.metadata.vulnerabilities
    if (-not $meta -or [int]$meta.total -eq 0) {
        Write-Host "No npm vulnerabilities reported by npm audit"
        return
    }

    $total = [int]$meta.total
    $summary = "npm: $total vulnerable package(s) " +
        "($([int]$meta.critical) critical, $([int]$meta.high) high, " +
        "$([int]$meta.moderate) moderate, $([int]$meta.low) low)"
    Write-Host $summary

    # One warning per vulnerable package, using the same TeamCity escaping as NuGet.
    $lines = @()
    if ($audit.vulnerabilities) {
        foreach ($name in ($audit.vulnerabilities.PSObject.Properties.Name | Sort-Object)) {
            $v = $audit.vulnerabilities.$name
            $line = "$name [$($v.severity)] $($v.range)"
            $lines += $line
            $escaped = ConvertTo-TeamCityEscaped $line
            Write-Host "##teamcity[message text='$escaped' status='WARNING']"
        }
    }

    # Tag the overall build status with the aggregate count.
    $escapedSummary = ConvertTo-TeamCityEscaped $summary
    Write-Host "##teamcity[buildStatus text='{build.status.text}, $escapedSummary']"

    # Write and publish a report artifact for download/archival.
    if (-not (Test-Path $dependency_report_dir)) {
        New-Item -ItemType Directory -Path $dependency_report_dir -Force | Out-Null
    }
    $reportPath = Join-Path $dependency_report_dir "npm-vulnerabilities.txt"
    $reportHeader = @(
        "Vulnerable npm packages report"
        "Working dir : $workingDir"
        "Build       : $fileVersion ($build_vcs_number)"
        "Summary     : $summary"
        ("-" * 60)
    )
    ($reportHeader + $lines) | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Wrote npm vulnerability report to $reportPath"
    Write-Host "##teamcity[publishArtifacts '$reportPath']"
}

function Invoke-Grunt {
    # Mirrors the old build.cake "Grunt" task: runs `grunt ci`, which compiles the
    # Kendo LESS under src\<project> into CSS *in the source tree*, so it must run
    # BEFORE the build copies web content into _PublishedWebsites.
    #
    # Presence-based: only runs when the project ships a Grunt setup
    # (tools\Grunt\package.json). $env:skip_grunt='true' force-skips it even so.
    $gruntDir = "$tools_dir\Grunt"
    $packageJson = "$gruntDir\package.json"

    if ($env:skip_grunt -match 'true') {
        Write-Host "skip_grunt set; skipping Grunt asset build"
        return
    }
    if (-not (Test-Path $packageJson)) {
        Write-Host "No $packageJson; skipping Grunt asset build"
        return
    }

    $node = Resolve-NodeTooling
    if (-not $node) {
        # The project defines front-end assets but there's no Node to build them;
        # shipping the site without the generated CSS would be a silent breakage.
        Write-Host "##teamcity[message text='Grunt setup present but Node/npm not found (PATH or tools\nodejs)' status='ERROR']"
        exit 1
    }

    # Run from the Grunt folder: package.json, gruntfile.js and the installed
    # node_modules all resolve relative to it (gruntfile paths are ../../src/...).
    Push-Location $gruntDir
    try {
        Write-Host "Installing Grunt dependencies (npm install) in $gruntDir"
        & $node.Npm install
        if ($LASTEXITCODE -ne 0) {
            Write-Host "##teamcity[message text='npm install failed' status='ERROR']"
            exit $LASTEXITCODE
        }

        # Report (but don't fail on) npm dependency vulnerabilities, same as NuGet.
        Report-NpmVulnerabilities -npm $node.Npm -workingDir $gruntDir

        # Invoke the locally-installed grunt-cli directly (matches the Cake task),
        # rather than relying on a global grunt being on PATH.
        $gruntBin = Join-Path $gruntDir "node_modules\grunt-cli\bin\grunt"
        Write-Host "Running grunt ci"
        & $node.Node $gruntBin ci
        if ($LASTEXITCODE -ne 0) {
            Write-Host "##teamcity[message text='grunt ci failed' status='ERROR']"
            exit $LASTEXITCODE
        }
    }
    finally {
        Pop-Location
    }
}

function Resolve-WebPublishingTasksAssembly {
    # Locates Microsoft.Web.Publishing.Tasks.dll (the assembly that provides the
    # TransformXml task) for whatever Visual Studio / MSBuild is installed on the
    # image. The DLL lives under <VSInstallDir>\MSBuild\Microsoft\VisualStudio\v<N>\Web\,
    # where <N> is the VS version (16.0=VS2019, 17.0=VS2022, 18.0=VS2026, ...). We
    # never pin a version: we take the newest v* folder found, so VS2022/VS2026 both
    # work with no code change.
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

    $searchRoots = @()
    if (Test-Path $vswhere) {
        $installPath = & $vswhere -latest -prerelease -products * `
            -requires Microsoft.Component.MSBuild `
            -property installationPath | Select-Object -First 1
        if ($installPath) { $searchRoots += "$installPath\MSBuild" }
    }
    # Fallbacks: any installed VS edition/year, then the standalone Build Tools /
    # legacy per-machine MSBuild location.
    $searchRoots += @(
        "${env:ProgramFiles}\Microsoft Visual Studio\*\*\MSBuild"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\*\*\MSBuild"
        "${env:ProgramFiles(x86)}\MSBuild"
    )

    foreach ($root in $searchRoots) {
        # Targeted glob (fast): only the Web publishing task folders, newest v* first.
        $dll = Get-ChildItem -Path "$root\Microsoft\VisualStudio\v*\Web\Microsoft.Web.Publishing.Tasks.dll" `
                -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1 -ExpandProperty FullName
        if ($dll) {
            Write-Host "Resolved Web publishing tasks assembly: $dll"
            return $dll
        }
    }

    Write-Host "##teamcity[message text='Microsoft.Web.Publishing.Tasks.dll not found; web.config transform will rely on the .proj VisualStudioVersion fallback' status='WARNING']"
    return $null
}

function Perform-WebConfigTransform {
    # Mirrors the old build.cake "WebConfigTransform" task.
    #
    # MSBuild precompiles the Web Site into $build_dir\_PublishedWebsites\<project>,
    # copying the source Web.config plus the Web.*.config transforms and the
    # WebConfigTransform.proj alongside it. This step applies the Release transform
    # to produce the deployed Web.config and strips the build-time-only files so the
    # published site ships a single, already-transformed Web.config.
    $publishedWebSitesFolder = "$build_dir\_PublishedWebsites"
    $projectFolder = "$publishedWebSitesFolder\$project"
    $transformProj = "$projectFolder\WebConfigTransform.proj"

    if ($env:skip_webconfig_transform -match 'true') {
        Write-Host "skip_webconfig_transform set; skipping web.config transform"
        return
    }
    if (-not (Test-Path $transformProj)) {
        Write-Host "No WebConfigTransform.proj at $transformProj; skipping web.config transform"
        return
    }

    # The .proj declares DefaultTargets="Demo" and runs TransformXml with paths
    # relative to its own folder (Web.config + Web.Release.config -> Web.Production.config).
    # Pass no /t so the default target runs (matches Cake's SetNoImplicitTarget(true)).
    #
    # Inject the resolved Microsoft.Web.Publishing.Tasks.dll so the transform works on
    # VS2022/VS2026 without the hard-coded v16.0 path. If resolution failed, the .proj
    # falls back to its own $(VisualStudioVersion)-based path.
    $msbuildArgs = @($transformProj, "/v:minimal", "/nodeReuse:false")
    $webTasksDll = Resolve-WebPublishingTasksAssembly
    if ($webTasksDll) {
        $msbuildArgs += "/p:WebPublishingTasksAssembly=$webTasksDll"
    }

    Write-Host "Transforming web.config via $transformProj"
    & $msbuild $msbuildArgs
    if (-not $?) { exit 1 }

    # Remove the build-time-only files that should not ship in the published site.
    $filesToRemove = @(
        "packages.config"
        "Web.Debug.config"
        "Web.Release.config"
        "Web.config"
        "WebConfigTransform.proj"
    )
    foreach ($fileName in $filesToRemove) {
        $filePath = Join-Path $projectFolder $fileName
        if (Test-Path $filePath) {
            Remove-Item -Path $filePath -Force
        }
    }

    # Promote the transformed output to be the deployed Web.config.
    $producedConfig = Join-Path $projectFolder "Web.Production.config"
    $deployedConfig = Join-Path $projectFolder "Web.config"
    if (Test-Path $producedConfig) {
        Move-Item -Path $producedConfig -Destination $deployedConfig -Force
        Write-Host "Published transformed Web.config to $deployedConfig"
    }
    else {
        Write-Host "##teamcity[message text='Web.Production.config was not produced by the transform' status='WARNING']"
    }
}

function Run-Tests {
    param (
        # Original Cake glob was ./src/**/bin/Release/*tests.dll.
        # Note: Perform-Build redirects output to $build_dir via /p:OutDir, so default to that.
        [string]$searchRoot = $build_dir,
        [string]$xunitVersion = $xunitVersion,
        [string[]]$skipPatterns = $testSkipPatterns
    )

    if ($env:skip_tests -match 'true') {
        Write-Host "skip_tests set; skipping unit tests"
        return
    }

    # An env override replaces (not extends) the in-script defaults, so a build
    # configuration can run everything by setting it to an empty value.
    if ($null -ne $env:test_skip_patterns) {
        $skipPatterns = @($env:test_skip_patterns -split '[,;]' |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ })
        Write-Host "test_skip_patterns override: $($skipPatterns -join ', ')"
    }

    # Find the test assemblies (case-insensitive match on *tests.dll) first, so we
    # skip the runner download entirely when there's nothing to test.
    # Force an array so a single match stays a string[] (not a scalar string that
    # would be enumerated character-by-character when passed to the runner).
    $candidates = @(Get-ChildItem -Path $searchRoot -Recurse -Filter "*tests.dll")

    # Partition the assemblies by runtime, because the two need different runners:
    #   * .NET Framework  -> xunit.console.x86.exe (cannot load .NET Core assemblies)
    #   * .NET (Core/5+)  -> dotnet vstest          (the Framework console cannot load these)
    # A .NET (Core/5+) build emits a sibling <name>.runtimeconfig.json; a .NET
    # Framework build does not — that file's presence is the discriminator.
    # Assemblies matching a skip regex (e.g. integration tests) are excluded from
    # both buckets; matching is case-insensitive on the name without extension.
    $classicAssemblies = @()
    $modernAssemblies = @()
    foreach ($candidate in $candidates) {
        $matched = @($skipPatterns | Where-Object { $candidate.BaseName -match $_ })
        if ($matched) {
            Write-Host "Skipping $($candidate.Name) (matched skip pattern '$($matched[0])')"
            continue
        }
        $runtimeConfig = [System.IO.Path]::ChangeExtension($candidate.FullName, ".runtimeconfig.json")
        if (Test-Path $runtimeConfig) { $modernAssemblies += $candidate.FullName }
        else { $classicAssemblies += $candidate.FullName }
    }

    if (-not $classicAssemblies -and -not $modernAssemblies) {
        Write-Host "No test assemblies to run under $searchRoot"
        return
    }

    # Track failures across both runners so every bucket runs (better reporting) before
    # the build fails, instead of aborting after the first failing runner.
    $testsFailed = $false

    # --- .NET (Core/5+) tests via dotnet vstest ---
    # vstest runs already-built test containers (no rebuild), symmetric with the
    # classic path below. TeamCity's .NET support supplies a 'teamcity' vstest logger
    # for live test reporting; only request it when actually running under TeamCity.
    if ($modernAssemblies) {
        Write-Host "Running .NET (SDK) tests via dotnet vstest:`n$($modernAssemblies -join "`n")"
        $vstestArgs = @($modernAssemblies)
        if ($env:TEAMCITY_VERSION) { $vstestArgs += "--logger:teamcity" }
        dotnet vstest $vstestArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Host "##teamcity[message text='.NET (SDK) unit tests failed' status='ERROR']"
            $testsFailed = $true
        }
    }

    # --- .NET Framework tests via xunit console ---
    if ($classicAssemblies) {
        # Ensure the xunit console runner is available (tools folder is wiped each build)
        $runnerPath = "$tools_dir\xunit.runner.console.$xunitVersion\tools\net481\xunit.console.x86.exe"
        if (-not (Test-Path $runnerPath)) {
            Write-Host "Restoring xunit.runner.console $xunitVersion"
            nuget install xunit.runner.console -Version $xunitVersion -OutputDirectory $tools_dir
            if (-not $?) { exit 1 }
        }

        Write-Host "Running .NET Framework tests via xUnit console:`n$($classicAssemblies -join "`n")"

        # xunit2 console accepts multiple assemblies in one invocation; -teamcity matches the Cake ArgumentCustomization.
        # Pass the array directly (no @ splat) so PowerShell expands each element as a separate argument.
        & $runnerPath $classicAssemblies -teamcity
        if ($LASTEXITCODE -ne 0) {
            Write-Host "##teamcity[message text='.NET Framework unit tests failed' status='ERROR']"
            $testsFailed = $true
        }
    }

    if ($testsFailed) { exit 1 }
}

function Generate-AssemblyInfoFile {
    if (Test-Path $commonAssemblyInfoFileName) {
        Write-Host "Writing Assembly Info to $commonAssemblyInfoFileName"
        $assemblyInfoContent = @"
using System.Reflection;

[assembly: AssemblyCompany("$companyName")]
[assembly: AssemblyProduct("$productName")]
[assembly: AssemblyVersion("$voVersion")]
[assembly: AssemblyFileVersion("$fileVersion")]
[assembly: AssemblyInformationalVersion("$fileVersion - $(($build_vcs_number.PadRight(10,"*")).Substring(0,6))")]
[assembly: AssemblyCopyright("$copyrightInfo")]
"@
        Set-Content -Path $commonAssemblyInfoFileName -Value $assemblyInfoContent -Encoding UTF8
    }
}

function Set-BuildProps {
    # SDK-style (.NET 8/9/10) projects version through MSBuild properties in
    # src\Directory.Build.props: the SDK's GenerateAssemblyInfo bakes Version /
    # AssemblyVersion / FileVersion into the assembly. Classic .NET Framework and C++
    # projects import this file too but IGNORE these properties for versioning, so they
    # use CommonAssemblyInfo.cs / version.inc instead (Generate-AssemblyInfoFile /
    # Create-VersionFile). Presence of the props file is the signal to use this path.
    if (-not (Test-Path $buildPropsFileName)) {
        Write-Host "No $buildPropsFileName; skipping Directory.Build.props versioning"
        return
    }

    Write-Host "Updating $buildPropsFileName (Version=$assemblyVersion, FileVersion=$fileVersion)"
    [xml]$xml = Get-Content -Path $buildPropsFileName -Raw

    # Update the FIRST <PropertyGroup> (create one if the file has none). Editing in
    # place preserves any other properties the repo keeps here (e.g.
    # PublishProjectReferences) rather than regenerating the file and dropping them.
    $propertyGroup = @($xml.Project.PropertyGroup)[0]
    if (-not $propertyGroup) {
        $propertyGroup = $xml.CreateElement('PropertyGroup')
        $xml.Project.AppendChild($propertyGroup) | Out-Null
    }

    # Parity with Generate-AssemblyInfoFile (the CommonAssemblyInfo.cs values), mapped to
    # the MSBuild property names the SDK's GenerateAssemblyInfo actually reads:
    #   AssemblyCompany              -> Company
    #   AssemblyProduct              -> Product
    #   AssemblyVersion              -> AssemblyVersion   ($voVersion / $assemblyVersion)
    #   AssemblyFileVersion          -> FileVersion
    #   AssemblyInformationalVersion -> InformationalVersion   (NB: NOT an element named
    #                                    AssemblyInformationalVersion, which the SDK ignores)
    #   AssemblyCopyright            -> Copyright
    # The commit hash is taken from $build_vcs_number (the TeamCity VCS number), NOT the
    # SDK's SourceRevisionId: the CI build runs against exported sources with no .git, so
    # SourceRevisionId is empty there and the SDK's usual "+<sha>" append never happens.
    # IncludeSourceRevisionInInformationalVersion=false disables that append entirely, so
    # the informational version is identical (and carries the real hash) in CI and locally.
    # Version / PackageVersion have no CommonAssemblyInfo equivalent but drive the SDK's
    # package version, so they are set for completeness.
    $versionValues = [ordered]@{
        Company                                     = $companyName
        Product                                     = $productName
        Copyright                                   = $copyrightInfo
        Version                                     = $assemblyVersion
        AssemblyVersion                             = $assemblyVersion
        FileVersion                                 = $fileVersion
        PackageVersion                              = $assemblyVersion
        InformationalVersion                        = "$fileVersion - $build_vcs_number"
        IncludeSourceRevisionInInformationalVersion = 'false'
    }

    # Set — or add, if absent — each element. This is deliberately more robust than
    # $xml.Project.PropertyGroup.Version = ...: that dotted assignment throws on a missing
    # element and misbehaves when the file has more than one PropertyGroup.
    foreach ($elementName in $versionValues.Keys) {
        $node = $propertyGroup.SelectSingleNode($elementName)
        if (-not $node) {
            $node = $xml.CreateElement($elementName)
            $propertyGroup.AppendChild($node) | Out-Null
        }
        $node.InnerText = $versionValues[$elementName]
    }

    # Drop an inert <AssemblyInformationalVersion> if a hand-written props file (or an
    # earlier version of this function) left one — the SDK reads InformationalVersion, so
    # the other element is dead weight and misleading.
    $staleInfo = $propertyGroup.SelectSingleNode('AssemblyInformationalVersion')
    if ($staleInfo) { $propertyGroup.RemoveChild($staleInfo) | Out-Null }

    $xml.Save($buildPropsFileName)
}

function Generate-TypeLibrary {
    param (
        [string]$inputFile,
        [string]$outputFile
    )

    if ($generateTypeLibrary) {
        Write-Host "Generating TLB from: $inputFile"
        /tools/tlbexp.exe `
            "$inputFile" `
            "/asmpath:c:\\app\\build" `
            "/asmpath:c:\\app\\src\\assemblies" `
            "/out:$outputFile"
        if (-not $?) { exit 1 }
    }
}

function Generate-InteropAssembly {
    param (
        [string]$inputFile,
        [string]$projectName
    )

    if ($generateInteropAssembly) {
        Write-Host "Generating Interop Assembly from: $inputFile"
        /tools/tlbimp.exe `
            "$inputFile" `
            "/out:c:\\app\\build\\$projectName.interop.dll" `
            "/namespace:ComputerWorkware.VitalObjects.Interop" `
            "/machine:Agnostic" `
            "/primary" `
            "/keyfile:/tools/ComputerWorkwareKey.snk" `
            "/copyright:`"$copyrightInfo`"" `
            "/company:$companyName" `
            "/product:$productName" `
            "/productversion:$voVersion" `
            "/asmversion:$voVersion" `
            "/trademark:`"${projectName}: $(($build_vcs_number.PadRight(10,"*")).Substring(0,6))`""
        if (-not $?) { exit 1 }
    }
}

function Resolve-PublishProject {
    # Selects the project(s) to publish as the artifact, in priority order:
    #   1. $env:publish_project (a project name / .csproj path, or a comma/semicolon list
    #      of them) always wins. Every listed project is published into the artifact.
    #   2. The single deployable app: an executable (<OutputType>Exe|WinExe</OutputType>)
    #      or an ASP.NET Core web app (Sdk="Microsoft.NET.Sdk.Web"). Test projects excluded.
    #   3. Otherwise (a library-only solution) the single non-test library "root" — the
    #      library not referenced by any other non-test project — so the artifact is a clean
    #      runtime closure rather than the raw build output (which still holds the test
    #      assemblies and the test-only packages: xunit, the test SDK, testhost).
    # Returns an array of .csproj full paths (usually one), or an empty array when there is
    # nothing to publish:
    #   * multiple ambiguous library roots (set publish_project to pick the deliverable(s)), or
    #   * a folder-based ASP.NET Web Site, whose artifact is produced by aspnet_compiler
    #     into build\_PublishedWebsites (there is no .csproj to publish).
    if ($env:publish_project) {
        # Accept one project or a comma/semicolon-separated list (same delimiters as
        # test_skip_patterns), and resolve each entry to its .csproj full path.
        $requested = @($env:publish_project -split '[,;]' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        $resolved = @()
        foreach ($entry in $requested) {
            $candidate =
                if (Test-Path $entry) { (Resolve-Path $entry).Path }
                else {
                    Get-ChildItem -Path $source_dir -Recurse -Filter "$entry.csproj" -ErrorAction SilentlyContinue |
                        Select-Object -First 1 -ExpandProperty FullName
                }
            if (-not $candidate) {
                Write-Host "##teamcity[message text='publish_project entry ''$entry'' not found' status='ERROR']"; exit 1
            }
            $resolved += $candidate
        }
        Write-Host "publish_project override: $($resolved -join '; ')"
        return $resolved
    }

    $projects = @(Get-ChildItem -Path $source_dir -Recurse -Include *.csproj, *.vbproj -ErrorAction SilentlyContinue)

    if ($publishAllProjects) {
        # Publish every non-test project. Publish-Artifact forces per-project subfolders for
        # this mode, so each gets its own clean closure (a shared dependency appears in each
        # dependent's folder, which is intended).
        $everyNonTest = @(
            foreach ($proj in $projects) {
                $text = Get-Content -Path $proj.FullName -Raw -ErrorAction SilentlyContinue
                if (-not $text) { continue }
                if (($proj.BaseName -match '(?i)tests?$') -or ($text -match '(?i)Microsoft\.NET\.Test\.Sdk')) { continue }
                $proj.FullName
            }
        )
        if ($everyNonTest.Count -eq 0) {
            Write-Host "publish_all_projects set, but no non-test projects found; leaving build output as-is"
            return @()
        }
        Write-Host "publish_all_projects: publishing all $($everyNonTest.Count) non-test project(s), each to its own subfolder"
        return $everyNonTest
    }

    $deployable = @()
    foreach ($proj in $projects) {
        $text = Get-Content -Path $proj.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $text) { continue }
        $isWeb = $text -match '(?i)Sdk\s*=\s*"Microsoft\.NET\.Sdk\.Web"'
        $isExe = $text -match '(?i)<OutputType>\s*(Exe|WinExe)\s*</OutputType>'
        # Exclude test projects (name ends in Test/Tests, or references the test SDK).
        $isTest = ($proj.BaseName -match '(?i)tests?$') -or ($text -match '(?i)Microsoft\.NET\.Test\.Sdk')
        if (($isWeb -or $isExe) -and -not $isTest) {
            $deployable += $proj.FullName
        }
    }

    if ($deployable.Count -eq 0) {
        # No deployable Exe/Web app. For a library-only solution, fall back to publishing
        # the non-test "root" library so the artifact is that library's clean runtime
        # closure (its own DLL + project references + package deps) instead of the raw
        # solution build, which still holds every test assembly plus the test-only packages.
        # The root is the non-test project NOT referenced by any other non-test project;
        # references from test projects don't count (a library used only by tests is still
        # a deliverable).
        $nonTest = @()
        $referencedByNonTest = @{}
        foreach ($proj in $projects) {
            $text = Get-Content -Path $proj.FullName -Raw -ErrorAction SilentlyContinue
            if (-not $text) { continue }
            if (($proj.BaseName -match '(?i)tests?$') -or ($text -match '(?i)Microsoft\.NET\.Test\.Sdk')) { continue }
            $nonTest += $proj
            foreach ($m in [regex]::Matches($text, '(?i)ProjectReference\s+Include\s*=\s*"([^"]+)"')) {
                $referencedByNonTest[[System.IO.Path]::GetFileNameWithoutExtension($m.Groups[1].Value)] = $true
            }
        }
        $roots = @($nonTest | Where-Object { -not $referencedByNonTest.ContainsKey($_.BaseName) })

        if ($roots.Count -eq 1) {
            Write-Host "No Exe/Web project; publishing library root '$($roots[0].BaseName)' as the artifact"
            return @($roots[0].FullName)
        }
        if ($roots.Count -gt 1) {
            # Ambiguous — do NOT guess, and do NOT fail (that would regress repos that build
            # fine today). Leave the raw build output as-is and ask for an explicit choice.
            # publish_project accepts a list, so the developer can name several of these.
            $names = ($roots | ForEach-Object { $_.BaseName }) -join ', '
            Write-Host "##teamcity[message text='No Exe/Web project and multiple library roots ($names); set env:publish_project (one name or a comma-separated list) to emit a clean artifact. Leaving build output as-is.' status='WARNING']"
        }
        return @()
    }
    if ($deployable.Count -gt 1) {
        Write-Host "##teamcity[message text='Multiple deployable projects found; set env:publish_project to choose. Candidates: $($deployable -join '; ')' status='ERROR']"
        exit 1
    }
    return @($deployable[0])
}

function Test-SdkStyleProject {
    param (
        [string]$projectPath
    )

    $head = (Get-Content -Path $projectPath -TotalCount 5 -ErrorAction SilentlyContinue) -join "`n"
    return $head -match '(?i)<Project\s+Sdk\s*='
}

function Publish-Artifact {
    # Reshapes $build_dir from the analysis/test build into the clean deployable.
    #
    # The solution build above (msbuild/dotnet build with /p:OutDir=$build_dir) exists
    # to run the SonarQube analyzers and to lay down the test assemblies for Run-Tests.
    # Its output folder is NOT a clean deployable: it also contains every test assembly,
    # the test-only packages (xunit, NSubstitute, AutoFixture) and any legacy .NET
    # Framework *.Core.dll builds a net8 app cannot load.
    #
    # SDK-style projects use dotnet publish to emit their runtime closure. Classic
    # .NET Framework projects use full MSBuild because dotnet publish invokes
    # unsupported ClickOnce tasks for those legacy project files.
    #
    # We reuse $build_dir so the TeamCity artifact path is unchanged. Wiping it here is
    # safe because Run-Tests (and the Generate-* steps) have already consumed the build.
    Write-Host "===== Publish-Artifact: reshaping $build_dir into the deployable artifact ====="
    $publishProjects = @(Resolve-PublishProject)
    if ($publishProjects.Count -eq 0) {
        # Library-only solution with no single root, or a folder-based ASP.NET Web Site
        # whose artifact is already in build\_PublishedWebsites. Nothing to publish and —
        # crucially — do NOT wipe $build_dir, which holds that output.
        Write-Host "Publish-Artifact: SKIPPED (no dotnet-publishable project detected); leaving $build_dir as-is"
        return
    }

    Write-Host "Publish-Artifact: publishing $($publishProjects -join '; ')"
    # Wipe once, then publish each requested project. The single-project case (the norm)
    # publishes FLAT into $build_dir, preserving the artifact path every repo/deploy relies
    # on. Each project goes into its own $build_dir\<ProjectName>\ subfolder when there is
    # more than one (independent deliverables can carry conflicting versions of a shared
    # dependency, and a flat merge would let one clobber the other), OR when
    # $publishInProjectSubfolders forces it even for a single project.
    Clear-BuildFolder
    $nestByProject = $publishInProjectSubfolders -or $publishAllProjects -or ($publishProjects.Count -gt 1)
    foreach ($publishProject in $publishProjects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($publishProject)
        $outDir = if ($nestByProject) { Join-Path $build_dir $projectName } else { $build_dir }
        if (Test-SdkStyleProject -projectPath $publishProject) {
            Write-Host "Publishing $projectName with dotnet publish (framework-dependent, $config) to $outDir"
            dotnet publish $publishProject -c $config -o $outDir
        }
        else {
            Write-Host "Publishing $projectName with MSBuild (classic .NET Framework, $config) to $outDir"
            & $msbuild /t:Rebuild /p:Configuration=$config /p:OutDir="$outDir\\" /p:MSBuildWarningsAsMessages=MSB3270 $publishProject /nodeReuse:false
        }
        if ($LASTEXITCODE -ne 0) {
            Write-Host "##teamcity[message text='publish failed for $publishProject' status='ERROR']"
            exit $LASTEXITCODE
        }
    }
}

# === Main Execution ===

Clear-BuildFolder

if (Test-Path $versionIncPath) {
    Write-Host "Creating version file at $versionIncPath"
    Create-VersionFile -major $major_ver -minor $minor_ver -build $buildVer -revision $revisionVer -filename $versionIncPath -projectName $project
}
else {
    Write-Host "Version file $versionIncPath does not exist. Skipping creation."
}

# Stamp the build version using whichever mechanism the repo carries. All are
# presence-guarded, so only the applicable one(s) act — and a mixed solution (a classic
# Framework project alongside an SDK project) can legitimately use more than one:
#   * src\Directory.Build.props  -> Set-BuildProps             (SDK-style .NET 8/9/10)
#   * src\CommonAssemblyInfo.cs   -> Generate-AssemblyInfoFile  (classic .NET Framework)
#   * src\<project>\version.inc   -> Create-VersionFile         (C++, handled above)
#Set-BuildProps
#Generate-AssemblyInfoFile

# Compile front-end assets (LESS -> CSS) into the source tree BEFORE the build, so
# the generated CSS is included when the Web Site is precompiled into _PublishedWebsites.
Invoke-Grunt

Set-ApplicationSettings

Restore-NuGetPackages -solutionPath $solutionPath

# Pick the MSBuild bitness up front (32-bit only when the solution has a Web Site
# project — see Resolve-MSBuild). All build paths below use $msbuild.
$msbuild = Resolve-MSBuild -solutionPath $solutionPath

# build-wrapper + the CFamily analyzer only apply to C/C++ projects. Detect a C++
# project file in the tree; anything else is treated as managed (C#) and scanned
# with the SonarScanner for .NET instead.
$isCppProject = $null -ne (
    Get-ChildItem -Path $source_dir -Recurse -Include *.vcxproj, *.vcproj -ErrorAction SilentlyContinue |
    Select-Object -First 1)

if ($performCodeScan) {
    if ($isCppProject) {
        Write-Host "C++ project detected: scanning with build-wrapper + CFamily"
        Perform-CodeScanAndBuild -solutionPath $solutionPath -projectName $project
    }
    else {
        # SonarScanner for .NET only works with MSBuild projects (.csproj/.vbproj
        # with a ProjectGuid). An ASP.NET Web Site project is folder-based and
        # compiled by aspnet_compiler, so it has no such project for the begin/end
        # integration to collect — analyse its raw sources with the standalone CLI.
        $hasMsbuildProject = $null -ne (
            Get-ChildItem -Path $source_dir -Recurse -Include *.csproj, *.vbproj -ErrorAction SilentlyContinue |
            Select-Object -First 1)

        if ($hasMsbuildProject) {
            Write-Host "Managed (C#) project detected: scanning with SonarScanner for .NET"
            Perform-CodeScanAndBuild-DotNet -solutionPath $solutionPath -projectName $project
        }
        else {
            Write-Host "ASP.NET Web Site project detected (no .csproj): scanning with standalone SonarScanner CLI"
            Perform-CodeScan-WebSite -solutionPath $solutionPath -projectName $project
        }
    }
}
else {
    Perform-Build -solutionPath $solutionPath
}

# Apply the Release web.config transform to the precompiled Web Site output and
# clean up the build-time-only config files (parity with the old build.cake).
Perform-WebConfigTransform

Run-Tests

Generate-TypeLibrary -inputFile $buildDllPath -outputFile "c:\\app\\build\\$project.tlb"
Generate-InteropAssembly -inputFile $buildDllPath -projectName $project

# Reshape $build_dir from the analysis/test build into the clean deployable artifact.
# Must be last: it wipes $build_dir and republishes only the app's runtime closure.
Publish-Artifact

Write-Host "##teamcity[setParameter name='dev.image.tag.used' value='$($env:DEVIMAGE_IMAGETAG)']"
Write-Host "Build image used: $($env:DEVIMAGE_IMAGETAG)"
