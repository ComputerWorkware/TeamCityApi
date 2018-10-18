# -----------------------------------------------
# Config Transform
# -----------------------------------------------
#
# Ver   Who                     When      What
# 1.0   Evolve Software Ltd     13-05-16  Initial Version

# https://stackoverflow.com/a/37204969/1689049

# Script Input Parameters
param (
    [ValidateNotNullOrEmpty()]
    [string] $ConfigurationFile = $(throw "-ConfigurationFile is mandatory, please provide a value."),
    [ValidateNotNullOrEmpty()]
    [string] $ApplicationSetting = $(throw "-ApplicationSetting is mandatory, please provide a value."),
    [ValidateNotNullOrEmpty()]
    [string] $ApplicationSettingValue = $(throw "-ApplicationSettingValue is mandatory, please provide a value.")
)

function Main() 
{
    $CurrentScriptVersion = "1.0"

    Write-Host "================== Config Transform - Version"$CurrentScriptVersion": START =================="

    # Log input variables passed in
    Log-Variables
    Write-Host

    try {
        $xml = [xml](get-content($ConfigurationFile))
        $conf = $xml.configuration
        $conf.appSettings.add | foreach { if ($_.key -eq $ApplicationSetting) { $_.value = $ApplicationSettingValue } }
        $xml.Save($ConfigurationFile)
    } 
    catch [System.Exception] {
        Write-Output $_
        Exit 1
    }

    Write-Host "================== Config Transform - Version"$CurrentScriptVersion": END =================="
}

function Log-Variables
{
    Write-Host "ConfigurationFile: " $ConfigurationFile
    Write-Host "ApplicationSetting: " $ApplicationSetting
    Write-Host "ApplicationSettingValue: " $ApplicationSettingValue
    Write-Host "Computername:" (gc env:computername)
}

Main