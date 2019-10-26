function Get-Crumb
{
	[CmdletBinding()]
	param 
	(
		[Parameter(Mandatory=$true)]
		[string]
		$JenkinsBaseUrl,
		[Parameter(Mandatory=$true)]
		[string]
		$Username,
		[Parameter(Mandatory=$true)]
		[string]
		$ApiToken
	)

	$credPair = $Username + ":" + $ApiToken
	$encodedPair = Convert-ToBase64String -OriginalString $credPair
	$cred = "Basic $encodedPair"

	$headers = @{
		Authorization = $cred
	}

	$crumbUrl = "$JenkinsBaseUrl/crumbIssuer/api/json"
	$crumb = Invoke-RestMethod -Uri $crumbUrl -Headers $headers

	return $crumb
}

function Convert-ToBase64String 
{
	param 
	(
		[Parameter(Mandatory=$true)]
		[string]
		$OriginalString
	)

	$bytes = [System.Text.Encoding]::UTF8.GetBytes($OriginalString)
	return [System.Convert]::ToBase64String($bytes)
}

<#
	My Function
#>
function Create-JenkinsNode
{
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory=$true)]
		[string] $JenkinsBaseUrl,
		[Parameter(Mandatory=$true)]
		[string] $NodeName,
		[Parameter(Mandatory=$true)]
		[string] $Username,
		[Parameter(Mandatory=$true)]
		[string] $ApiToken
	)

	Write-Host $JenkinsBaseUrl
}