function Get-CloudFileHash
{
    [CmdletBinding(DefaultParameterSetName = "CloudPath")]
    param(
        [Parameter(Mandatory, ParameterSetName="CloudPath", Position = 0)]
        [System.String[]]
        $CloudPath,

        [Parameter(Mandatory, ParameterSetName="LiteralCloudPath", ValueFromPipelineByPropertyName = $true)]
        [Alias("PSPath")]
        [System.String[]]
        $LiteralCloudPath,
        
        [Parameter(Mandatory, ParameterSetName="Stream")]
        [System.IO.Stream]
        $InputStream,

        [ValidateSet("SHA1", "SHA256", "SHA384", "SHA512", "MACTripleDES", "MD5", "RIPEMD160")]
        [System.String]
        $Algorithm="SHA256"
    )
    
    begin
    {
        # Construct the strongly-typed crypto object
        $hasher = [System.Security.Cryptography.HashAlgorithm]::Create($Algorithm)
    }
    
    process
    {
        if($PSCmdlet.ParameterSetName -eq "Stream")
        {
            GetCloudStreamHash -InputStream $InputStream -RelatedPath $null -Hasher $hasher
        }
        else
        {
            $pathsToProcess = @()
            if($PSCmdlet.ParameterSetName  -eq "LiteralCloudPath")
            {
                $pathsToProcess += Resolve-Path -LiteralPath $LiteralCloudPath | Foreach-Object ProviderPath
            }
            if($PSCmdlet.ParameterSetName -eq "CloudPath")
            {
                $pathsToProcess += Resolve-Path $CloudPath | Foreach-Object ProviderPath
            }

            foreach($filePath in $pathsToProcess)
            {
                if(Test-Path -LiteralPath $filePath -PathType Container)
                {
                    continue
                }

                try
                {
                    # Read the file specified in $FilePath as a Byte array
					[byte[]]$content = Get-Content $filePath -Encoding Byte -ReadCount 0
                    [system.io.stream]$stream = New-Object System.IO.MemoryStream (,$content)
                    $hash = GetCloudStreamHash -InputStream $stream -RelatedPath $filePath -Hasher $hasher
					$hash.Path = $filePath
					$hash
                }
                catch [Exception]
                {
                    $errorMessage = [Microsoft.PowerShell.Commands.UtilityResources]::FileReadError -f $filePath, $_
                    Write-Error -Message $errorMessage -Category ReadError -ErrorId "FileReadError" -TargetObject $filePath
                    return
                }
                finally
                {
                    if($stream)
                    {
                        $stream.Close()
                    }
                }                            
            }
        }
    }
}

function GetCloudStreamHash
{
    param(
        [System.IO.Stream]
        $InputStream,

        [System.String]
        $RelatedPath,

        [System.Security.Cryptography.HashAlgorithm]
        $Hasher)

    # Compute file-hash using the crypto object
    [Byte[]] $computedHash = $Hasher.ComputeHash($InputStream)
    [string] $hash = [BitConverter]::ToString($computedHash) -replace '-',''
	
    if ($RelatedPath -eq $null)
    {
        $retVal = [PSCustomObject] @{
            Algorithm = $Algorithm.ToUpperInvariant()
            Hash = $hash
        }
        $retVal.psobject.TypeNames.Insert(0, "Microsoft.Powershell.Utility.FileHash")
        $retVal
    }
    else
    {
        $retVal = [PSCustomObject] @{
            Algorithm = $Algorithm.ToUpperInvariant()
            Hash = $hash
            Path = $RelatedPath
        }
        $retVal.psobject.TypeNames.Insert(0, "Microsoft.Powershell.Utility.FileHash")
        $retVal

    }
}
