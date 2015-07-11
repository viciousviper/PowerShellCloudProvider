# PowerShellCloudProvider
**PowerShellCloudProvider** is an extensible Windows PowerShell Filesystem Provider for cloud storage services with transparent client-side encryption.

## Objective

In these times when everyone's online activities are subject to omnipresent surveillance by governmental and other outright criminal institutions it is more important that ever to protect your private data before storing it in the cloud.<br/>
PowerShellCloudProvider addresses this situation by offering transparent client-side encrypted access to various cloud storage services via Windows PowerShell's pluggable provider mechanism. The virtual drives created by PowerShellCloudProvider can then be managed via the entire set of file-related PowerShell *Cmdlets*.

## Features

- connection and disconnection of virtual drives via standard PowerShell Cmdlets
  - `New-PSDrive`, `Remove-PSDrive`, `Get-PSDrive`
- support of all standard file-related PowerShell-Cmdlets<br/>(actual supported features depend on the capabilities exposed by any specific cloud storage service)
  - `New-Item`, `Remove-Item`, `Get-Item`, `Get-ChildItem`, `Test-Path`
  - `Get-Content`, `Set-Content`, `Clear-Content`
  - `Copy-Item`, `Move-Item`, `Rename-Item`
- access to various cloud storage services via pluggable gateway libraries
  - see below for currently supported set of services
- transparent AES-encryption of all uploaded files
  - the file format uploaded to cloud storage is compatible with the [AESCrypt](http://www.aescrypt.com/) application
- integrated authentication via [OAuth2](https://en.wikipedia.org/wiki/OAuth "OAuth2") or alternative authentication schemes as defined by cloud storage services<br />(actual behavior dependent on capabilities of specific cloud storage service)
  - retention of refresh tokens for automatic login

## Supported Cloud storage services

Consideration of a cloud storage service as a target for PowerShellCloudProvider depends on these conditions:
- free storage space quota of at least 10 GB
- file expiration period no shorter than 90 days for free users
- availability of a .NET-accessible API under a non-invasive open source license (Apache, MIT, MS-PL)

Currently the following cloud storage services are supported in PowerShellCloudProvider via the specified API libraries:

| Cloud storage service                                       | API library                                                      | sync/async | status    |
| :---------------------------------------------------------- | :--------------------------------------------------------------: | :--------: | :-------: |
| *(local files)*                                             | *System.IO (.NET Framework)*                                     | *sync*     |           |
| [Microsoft OneDrive](https://onedrive.live.com/ "OneDrive") | [OneDriveSDK](https://github.com/OneDrive/onedrive-explorer-win)  | async      | official  |
| [Google Drive](https://drive.google.com/ "Google Drive")    | [Google Apis](https://github.com/google/google-api-dotnet-client) | async      | official  |
| [Box](https://app.box.com/ "Box")                           | [Box.V2](https://github.com/box/box-windows-sdk-v2)               | async      | official  |
| [Copy](https://www.copy.com/ "Copy")                        | [CopyRestAPI](https://github.com/saguiitay/CopyRestAPI)           | async      | 3rd party |
| [MEGA](https://mega.co.nz/ "MEGA")                          | [MegaApiClient](https://github.com/gpailler/MegaApiClient)        | sync       | 3rd party |
| [pCloud](https://www.pcloud.com/ "pCloud")                  | [pCloud.NET](https://github.com/nirinchev/pCloud.NET)             | async      | 3rd party |

## System Requirements

- Platform
  - .NET 4.6
  - Windows PowerShell 3.0
- Operating system
  - tested on Windows 8.1 x64 and Windows Server 2012 R2
  - expected to run on Windows 7/8/8.1 and Windows Server 2008(R2)/2012(R2)

## Local compilation

Several cloud storage services require additional authentication of external applications for access to cloud filesystem contents.<br/>For cloud storage services with this kind of authentication policy in place you need to take the following steps before compiling PowerShellCloudProvider locally:
- register for a developer account with the respective cloud service
- create a cloud application configuration with sufficient rights to access the cloud filesystem
- enter the service-provided authentication details into the prepared fields in the `Secrets` class of the affected PowerShellCloudProvider gateway project

## Release Notes

2015-07-10 Version 1.0.0.0 - Initial release

## Usage

### Import PowerShell Cmdlets

Execute the following statements in the *$TargetDir* of *PowerShellCloudProvider* (e.g. *"...\PowerShellCloudProvider\bin\Release"*).

**Note:** All gateway assemblies for specific cloud services are expected to be placed in a subdirectory named *"Gateways"* beneath this directory.

```posh
# import cmdlets
Import-Module .\IgorSoft.PowerShellCloudProvider.dll
Import-Module .\IgorSoft.PowerShellCloudProvider.psd1
Update-FormatData .\IgorSoft.PowerShellCloudProvider.ps1xml

Import-Module .\IgorSoft.PowerShellCloudProvider.Utility.dll
Import-Module .\IgorSoft.PowerShellCloudProvider.Utility.psm1
```

### Open Cloud drive

```posh
# declare dummy password (not actually used for authentication)
$password = ConvertTo-SecureString ' ' -AsPlainText -Force

# declare user name and symmetric encryption key
$credentials = New-Object PSCredential ('CloudUser', $password)
$encryptionKey = 'MySecret'
# open connection to PowerShell drive
New-PSDrive -PSProvider CloudFileSystem -Name O -Root onedrive -Description 'OneDrive CloudUser' -Credential $credentials -EncryptionKey $encryptionKey
```

At this point (depending on the specific service gateway) a login window may pop up and allow you to provide your cloud storage account credentials. Once a login has succeeded the returned *RefreshToken* is stored by PowerShellCloudProvider, facilitating an automatic login to this account for the duration period of the refresh token.

Transparent AES encryption of uploaded files requires an encryption key of at least 8 characters in lenght. If you fail to provide an `-EncryptionKey` parameter in the call to `New-PSDrive`, the virtual drive will be unencrypted. PowerShellCloudProvider will warn you about the resulting security breach.

**Notes:**
 - Due to the nature of the widely used OAuth2 authentication scheme it is not possible to provide login details entirely via PowerShell commandline parameters.
 - It is possible to open multiple virtual drives on the same cloud storage account in parallel (e.g. encrypted and in the plain at the same time). However, the effects of a write operation in one of these drives will not be visible to the other drives until an explicit refresh via `Get-ChildItem -Force` due to the non-existance of update events from the cloud storage services.


### Upload files to cloud storage

```posh
# create upload folder in virtual cloud drive
New-Item -ItemType Directory 'O:\Upload'

# upload a single file
Get-Item 'C:\Windows\System32\cmd.exe' | Foreach-Object { New-Item -ItemType File 'O:\Upload\cmd.exe.aes' -Value $_.OpenRead() }

# upload all image files in a directory
Get-ChildItem 'C:\Temp' -Filter '*.jpg' | Foreach-Object { New-Item -ItemType File 'O:\Upload\Pictures\$_.Name.aes' -Value $_.OpenRead() }
```

**Note:** The SHA-1 hash value returned from the above PowerShell statement will differ from the file hash value reported by the cloud storage service due to the transparent AES encryption configured on drive O:.

### Verify file content by cryptographic hashes

```posh
# get file hash from local filesystem
Get-ChildItem 'C:\Temp' -Filter '*.jpg' | Get-FileHash -Algorithm SHA1 | Format-Table -Property Hash,Path -AutoSize

# get file hash from cloud storage
Get-ChildItem 'O:\Upload\Pictures' -Filter '*.jpg.aes' | Get-CloudFileHash -Algorithm SHA1 | Format-Table -Property Hash,Path -AutoSize
```

The `Get-CloudFileHash` Cmdlet is an adaptation of the system-provided `Get-FileHash` that removes the latter's restriction to local `System.IO.FileInfo` objects. All hashing allgorithms available in `Get-FileHash` can also be used in `Get-CloudFileHash`.

### Download files from cloud storage

```posh
# create local download folder
New-Item -ItemType Directory 'C:\Temp\Download'

# download a single file
Get-Item 'O:\Upload\cmd.exe.aes' | Foreach-Object { $content = Get-Content $_ -Encoding Byte -ReadCount 0; [System.IO.File]::WriteAllBytes("C:\Temp\Download\cmd.exe", $content) }

# upload all image files in a directory
Get-ChildItem 'O:\Upload\Pictures' -Filter '*.jpg.aes' | Foreach-Object { $content = Get-Content $_ -Encoding Byte -ReadCount 0; $filename = $_.Name.Replace('.aes','') ; [System.IO.File]::WriteAllBytes($filename, $content) }
```

### Further examples

See the file *.\InstallProvider.ps1* for further usage examples.

## Utilities

The following utilitiy Cmdlets are included in PowerShellCloudProvider:
- `Get-CloudGateway` lists all available cloud storage services
- `Rename-Path` creates a relative path name based on file name components (e.g. trailing digits) as a quick way of partitioning large directories into groups of similarly named files

***
## Acknowledgements

The realization of PowerShellCloudProvider would have been impossible without the excellent previous work of beefarino on the PowerShell Provider Framework [p2f](https://github.com/beefarino/p2f "PowerShell Provider Framework").
