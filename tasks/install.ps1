(new-object net.webclient).DownloadFile('https://download.microsoft.com/download/4/6/1/46116DFF-29F9-4FF8-94BF-F9BE05BE263B/packages/DotNetCore.1.0.0.RC2-SDK.Preview1-x64.exe','core.exe')

core.exe /install /quiet /norestart

ls src | % { &"dotnet restore" $_.FullName }
ls test | % { &"dotnet restore" $_.FullName }