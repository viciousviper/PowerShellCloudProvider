Import-Module .\IgorSoft.PowerShellCloudProvider.dll
Import-Module .\IgorSoft.PowerShellCloudProvider.psd1
Update-FormatData .\IgorSoft.PowerShellCloudProvider.ps1xml

Import-Module $PSScriptRoot\PowerShellCloudProvider.Utility\bin\Debug\IgorSoft.PowerShellCloudProvider.Utility.dll
Import-Module $PSScriptRoot\PowerShellCloudProvider.Utility\IgorSoft.PowerShellCloudProvider.Utility.psm1

$password = ConvertTo-SecureString ' ' -AsPlainText -Force

$credentials = New-Object PSCredential ('PSCP', $password)
$encryptionKey = 'My_OneDrive_Secret&I'
New-PSDrive -PSProvider CloudFileSystem -Name R -Root onedrive -Description 'OneDrive PSCP' -Credential $credentials -EncryptionKey $encryptionKey

$credentials = New-Object PSCredential ('PSCP', $password)
$encryptionKey = 'My_GDrive_Secret&I'
New-PSDrive -PSProvider CloudFileSystem -Name S -Root gdrive -Description 'GDrive PSCP' -Credential $credentials -EncryptionKey $encryptionKey

$credentials = New-Object PSCredential ('PSCP', $password)
$encryptionKey = 'My_Box_Secret&I'
New-PSDrive -PSProvider CloudFileSystem -Name T -Root box -Description 'Box PSCP' -Credential $credentials -EncryptionKey $encryptionKey

$credentials = New-Object PSCredential ('PSCP', $password)
$encryptionKey = 'My_Copy_Secret&I'
New-PSDrive -PSProvider CloudFileSystem -Name U -Root copy -Description 'Copy PSCP' -Credential $credentials -EncryptionKey $encryptionKey

$credentials = New-Object PSCredential ('PSCP', $password)
$encryptionKey = 'My_Mega_Secret&I'
New-PSDrive -PSProvider CloudFileSystem -Name V -Root mega -Description 'Mega PSCP' -Credential $credentials -EncryptionKey $encryptionKey

$credentials = New-Object PSCredential ('PSCP', $password)
$encryptionKey = 'My_pCloud_Secret&I'
New-PSDrive -PSProvider CloudFileSystem -Name W -Root pcloud -Description 'pCloud PSCP' -Credential $credentials -EncryptionKey $encryptionKey

# upload a single file
#
# Get-Item 'C:\Windows\System32\cmd.exe' | Foreach-Object { New-Item -ItemType File R:\cmd.exe.aes -Value $_.OpenRead() }

# upload local pictures directory to cloud backup and partition into subdirectories, e.g.
#   C:\Pictures\Originals\IMG_1256.jpg -> S:\PictureBackup\1000\1200\IMG_1256.jpg.aes
#   C:\Pictures\Originals\IMG_2163.jpg -> S:\PictureBackup\2000\2100\IMG_2163.jpg.aes
#
# Get-ChildItem "C:\Pictures\Originals" -Recurse -Filter *.jpg | Sort-Object -Property Name | ForEach { $item = Rename-Path $_.Name -Replacement 'S:\PictureBackup\${TBS}${Filename}.aes' ; If (!(Test-Path $item)) { New-Item $item -ItemType File -Value $_.OpenRead() -Force } }

# verify cloud files via cryptoraphic hashes
# 
# Get-ChildItem 'S:\PictureBackup' -Recurse | Get-CloudFileHash -Algorithm SHA1 | Format-Table -Property Hash,Path -AutoSize

