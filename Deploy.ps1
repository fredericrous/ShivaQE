
$vs2012 = "C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"

echo "Build ShivaQE..."
Start-Process -FilePath $vs2012 -ArgumentList ('ShivaQE.sln', '/Build Release') -Wait

echo "Package..."
New-Item -Force -ItemType directory -Path 'deploy_tmp'
New-Item -Force -ItemType directory -Path 'deploy_tmp\ShivaQEmaster'
New-Item -Force -ItemType directory -Path 'deploy_tmp\ShivaQEviewer'

$source = $PSScriptRoot + '\ShivaQEmaster\bin\Release'
$dest = 'deploy_tmp\ShivaQEmaster'
$exclude = @('*pdb','*xml', '*vshost.exe*', '*log', '*log.*', '*json', 'PsExec.exe')
Get-ChildItem -Force $source -Recurse -Exclude $exclude | Copy-Item -Destination {Join-Path $dest $_.FullName.Substring($source.length)} -Force

$source = $PSScriptRoot + '\ShivaQEviewer\bin\Release'
$dest = 'deploy_tmp\ShivaQEviewer'
Get-ChildItem -Force $source -Recurse -Exclude $exclude | Copy-Item -Destination {Join-Path $dest $_.FullName.Substring($source.length)} -Force

Copy-Item Readme.md deploy_tmp\Readme.txt

Remove-Item 'gh-pages\ShivaQE.zip' -Force

Add-Type -Assembly "System.IO.Compression.FileSystem" ;
[System.IO.Compression.ZipFile]::CreateFromDirectory($PSScriptRoot + '\deploy_tmp', "gh-pages\ShivaQE.zip") ;

Remove-Item 'deploy_tmp' -Recurse