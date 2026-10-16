param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString,
    [switch] $Disable
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if ($ConnectionString -notmatch 'InstrumentationKey=[0-9a-fA-F-]{36}(;|$)') {
    throw 'Supply the Application Insights connection string, not a SQL connection string.'
}
$relativePaths = @(
    'src/DogWorld.Desktop/appsettings.Local.json',
    'src/DogWorld.Api/appsettings.Local.json',
    'src/DogWorld.Breeds.Api/appsettings.Local.json',
    'src/DogWorld.Vaccinations.Api/appsettings.Local.json',
    'src/DogWorld.Web/wwwroot/telemetry.local.json'
)
foreach ($relativePath in $relativePaths) {
    $path = Join-Path $repoRoot $relativePath
    $settings = if (Test-Path -LiteralPath $path) { Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
    if (-not $settings.ContainsKey('ApplicationInsights')) { $settings.ApplicationInsights = @{} }
    $settings.ApplicationInsights.ConnectionString = $ConnectionString
    $settings.ApplicationInsights.Enabled = -not $Disable
    $settings.ApplicationInsights.SamplingRatio = 1.0
    $settings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output "Configured $relativePath"
}
Write-Output 'Restart/rebuild all five applications. These configuration files are ignored by Git.'
Write-Output 'Browser configuration is public; never put credentials in telemetry.local.json.'

