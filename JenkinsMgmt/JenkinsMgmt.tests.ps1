Import-Module -Name ".\JenkinsMgmt\JenkinsMgmt.psm1" -Force

Describe "Get-Crumb" {
	Context "JenkinsMgmt" {
		It "gets crumb from the server" {
			$crumb = Get-Crumb -JenkinsBaseUrl "http://jenkins.homenet.iriumi.ad" -Username "hiriumi" -ApiToken "1124d64666dda45ea4200d6b81e616baf8"
			Write-Host $crumb
		}
	}
}

Describe "Convert-ToBase64String" {
	Context "JenkinsMgmt" {
		It "return base64 string" {
			$b64str = Convert-ToBase64String -OriginalString "Hello World"
			Write-Host $b64str
		}
	}
}