param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Breeds', 'Vaccinations')]
    [string] $Service,
    [ValidateSet('Unavailable', 'Timeout', 'Recover', 'Off')]
    [string] $Scenario = 'Unavailable'
)
$ErrorActionPreference = 'Stop'
$previous = $env:ResilienceLab__Scenario
try {
    $env:ResilienceLab__Scenario = $Scenario
    $project = Join-Path (Split-Path $PSScriptRoot -Parent) "src/DogWorld.$Service.Api"
    Write-Output "Starting $Service in Development with scenario $Scenario. Stop its existing instance first."
    Write-Output 'Recover fails the first two API requests after startup; use one dog-details request at a time.'
    dotnet run --project $project --launch-profile http
    if ($LASTEXITCODE -ne 0) { throw 'The downstream service exited unsuccessfully.' }
} finally {
    if ($null -eq $previous) { Remove-Item Env:ResilienceLab__Scenario -ErrorAction SilentlyContinue }
    else { $env:ResilienceLab__Scenario = $previous }
}
