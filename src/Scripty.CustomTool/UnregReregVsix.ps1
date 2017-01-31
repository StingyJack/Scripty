#quick and dirty to remove and re-add the Scripty vsix
$cur = $PSScriptRoot

cd $env:VS140COMNTOOLS\..\IDE

.\vsixInstaller.exe /a /u:Scripty.CustomTool

.\VSIXInstaller.exe /a $cur\bin\Debug\Scripty.vsix




 