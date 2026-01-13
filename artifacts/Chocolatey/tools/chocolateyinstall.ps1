$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url = 'https://github.com/Ghenghis/Svony-Browser/releases/download/v7.0.0/SvonyBrowser_v7.0.0_Portable.zip'

$packageArgs = @{
  packageName   = 'svonybrowser'
  unzipLocation = $toolsDir
  url           = $url
  checksum      = 'CHECKSUM_PLACEHOLDER'
  checksumType  = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs
