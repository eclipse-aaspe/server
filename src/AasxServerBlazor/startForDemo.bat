set Kestrel__Endpoints__Http__Url=http://*:5002
set AASREGISTRY=http://localhost:5001
set IFRAMEPATH=https://dpp40-2-v2.industrialdigitaltwin.org/dashboard/submodelViewV3.html
cd C:\Users\Y97CO3\Git\aasx-server-db\src\AasxServerBlazor\bin\Debug\net8.0
AasxServerBlazor.exe --no-security --data-path "C:\Users\Y97CO3\OneDrive - PHOENIX CONTACT GmbH & Co. KG\04_Abteilung_DI\02_Projekte\02_Aasx_Server\01_AASX-Server_AASen\aasx_bachelor\ShowCase\PCF EIS\eisview" --external-blazor http://localhost:5002
