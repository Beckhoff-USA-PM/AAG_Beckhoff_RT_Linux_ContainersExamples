# Print out the Environment-Variables (IConfiguation settings)
#Get-ChildItem env:

# Only for debugging purposes (when the TcXaeMgmt is provided as COPY instruction in dockerfile)
$debugModuleExist = Test-Path -path .\TcXaeMgmt
if ($debugModuleExist)
{
    import-module -name .\TcXaeMgmt\TcXaeMgmt.psm1
    update-FormatData -AppendPath .\TcXaeMgmt\TcXaeMgmt.format.ps1xml
}

get-module TcXaeMgmt

# Wait, the router/broker needs time to start ...
Start-Sleep -Seconds 5

# Show the AmsRouter Endpoint (the Loopback settings)
Write-Host 'LocalEndpoint:'
Get-AmsRouterEndpoint
# Show the Local AmsNetId
Write-Host 'Local AmsNetId:'
Get-AmsNetId

Write-Host '+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++'
Write-Host 'Attach to the powershell console within this docker instance with:'
Write-Host 'PS> docker exec -it [containerID] pwsh'
Write-Host '+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++'

#Connect to the AdsTestServer on Port 10000
# Use HOST_AMS_NETID environment variable if provided, otherwise use local connection
$targetNetId = $env:HOST_AMS_NETID
if ($targetNetId) {
    Write-Host "Connecting to host AMS NetID: $targetNetId port 851"
    $s = New-TcSession -NetId $targetNetId -port 851
}
Write-Host 'Connection established to:'
Write-Host $s

# Testing connections for 10000 Seconds
while($true)
{
    $state = get-AdsState -session $s -StateOnly
    Write-Host "[PowershellClient] State of Server '$($s.NetId):$($s.Port)' is: $state"
    Start-Sleep -Seconds 1
}
