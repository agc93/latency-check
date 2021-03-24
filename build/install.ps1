$binPath = Join-Path $PSScriptRoot "LatencyCheck.Service.exe"

$SID = [System.Security.Principal.WellKnownSidType]::NetworkServiceSid 
$Account = new-object system.security.principal.securityidentifier($SID, $null) 
$NetworkServiceName = $Account.Translate([system.security.principal.ntaccount]).value
$secpasswd = (new-object System.Security.SecureString)
$mycreds = New-Object System.Management.Automation.PSCredential ($NetworkServiceName, $secpasswd)

$params = @{
    Name = "LatencyCheck"
    BinaryPathName = "`"$binPath`""
    DisplayName = "LatencyCheck"
    StartupType = "Automatic"
    Description = "Background worker for LatencyCheck process monitoring."
    Credential = $mycreds
  }
Write-Output $params
New-Service @params

$configPath = Join-Path $env:APPDATA "LatencyCheck"
$configFilePath = Join-Path $configPath "checks.json"
if (-Not (Test-Path $configPath -PathType Container)) {
    New-Item -Path $configPath -ItemType Directory
}
if (-Not (Test-Path $configFilePath -PathType leaf))
{
    New-Item -Path $configFilePath -ItemType File -Value "{}"
}
