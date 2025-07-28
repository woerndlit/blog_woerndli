[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $certificatestring,
    # Parameter help description
    [Parameter()]
    [string]
    $certificatepw,
    [Parameter()]
    [string]
    $appid
)

### prepare connection
[System.IO.File]::WriteAllBytes('.\cert.pfx',[System.Convert]::FromBase64String($certificatestring))
Connect-IPPSSession -CertificateFilePath ".\cert.pfx" -CertificatePassword "$($certificatepw)" -AppId "$($appid)" -Organization "woernd.li"

###### Backup DLP Policies and Rules ########

$backuppath = ".\backup"

$dlppolicies = Get-DlpCompliancePolicy
foreach ($dlppolicy in $dlppolicies) {
    New-Item -Path "$($backuppath)" -Name "$($dlppolicy.Guid)" -ItemType Directory -Force
    $dlppolicy | ConvertTo-Json -Depth 100 | Out-File -FilePath "$($backuppath)\$($dlppolicy.Guid)\$($dlppolicy.Guid).json"
    $rules = Get-DlpComplianceRule -IncludeExecutionRuleGuids $true -Policy "$($dlppolicy.Guid)"
    foreach ($rule in $rules) {
        $rule | ConvertTo-Json -Depth 100 | Out-File -FilePath "$($backuppath)\$($dlppolicy.Guid)\rule_$($rule.Guid).json"
    }
}
