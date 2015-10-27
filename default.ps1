Framework "4.5.1"

#required parameters:
#$major_ver
#$minor_ver
#$build_counter
#$build_vcs_number

properties {
    $project = "TeamCityApi"
    
    $companyName = "Computer Workware Inc."
    $productName = "VITAL Objects"
    $date = [DateTime]::Now
    $global:config = "release"

    #version
    $buildVer = [string]::Format("{0}{1:00}{2:00}",$date.Year-$initial_year,$date.Month,$date.Day)
    $revisionVer = if ($build_counter) { $build_counter  } else { "0" }
    $voVersion = "$major_ver.$minor_ver.0.0"
    $fileVersion = "$major_ver.$minor_ver.$buildVer.$revisionVer"
    $assemblyVersion = $voVersion


    #general dirs
    $base_dir = resolve-path .
    $build_dir = "$base_dir\build"
    $source_dir = "$base_dir\src"
    $tools_dir = "$base_dir\tools"
    $test_dir = "$build_dir\test"
    $result_dir = "$build_dir\results"
    $pubsites_dir = "$build_dir\_PublishedWebsites"
    $webapp_dir = "$pubsites_dir\$project"
}

task default -depends compile, dist
task local -depends compile, test
task full -depends local, dist
task ci -depends clean, release, commonAssemblyInfo, local

task clean {
	delete_directory "$build_dir"
}

task release {
    $global:config = "release"
}

task compile -depends clean { 
    exec { msbuild /t:Clean /t:Build /p:Configuration=$config /p:OutDir=$build_dir /p:TeamCityApiPath="$build_dir\" $source_dir\$project.sln }
    if ( -not (Test-Path "$source_dir\TeamCityApi\bin\Debug\"))
    {
        New-Item -ItemType directory -Path "$source_dir\TeamCityApi\bin\Debug\"
    }
    cp "$build_dir\TeamCityApi.dll" "$source_dir\TeamCityApi\bin\Debug\TeamCityApi.dll"
}

task commonAssemblyInfo {
    create-assemblyInfo $fileVersion $assemblyVersion "$source_dir\CommonAssemblyInfo.cs"
}

task test {
    create_directory "$build_dir\results"

    @(get-childitem $build_dir -recurse -include *tests.dll) | `
    Foreach-Object{
        Write-Host $tools_dir\xunit\xunit.console.clr4.exe $_.FullName /teamcity
        exec { & $tools_dir\xunit\xunit.console.clr4.exe $_.FullName /teamcity }
    }
}

# -------------------------------------------------------------------------------------------------------------
# generalized functions 
# --------------------------------------------------------------------------------------------------------------
function global:delete_directory($directory_name)
{
  rd $directory_name -recurse -force  -ErrorAction SilentlyContinue | out-null
}

function global:create_directory($directory_name)
{
  mkdir $directory_name  -ErrorAction SilentlyContinue  | out-null
}

function global:create-assemblyInfo($fver, $aver, $filename)
{
"using System;
using System.Reflection;
using System.Runtime.InteropServices;

// **** IMPORTANT ****
// During the build process on the CI Server, this file will be regenerated.
// The information in the file is still valid for local builds as the file will not be regenerated locally.

[assembly: AssemblyConfiguration("""")]
[assembly: AssemblyCompany(""Computer Workware Inc."")]
[assembly: AssemblyCopyright(""Copyright Computer Workware Inc. "+[DateTime]::Now.Year+""")]
[assembly: AssemblyTrademark("""")]
[assembly: AssemblyCulture("""")]
[assembly: AssemblyVersion(""$aver"")]
[assembly: AssemblyTitle(""Vcs: $build_vcs_number"")]
[assembly: AssemblyInformationalVersion(""$fver"")]
[assembly: AssemblyFileVersion(""$fver"")]"  | out-file $filename -encoding "ASCII"    

}