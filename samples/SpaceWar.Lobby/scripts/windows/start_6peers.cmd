dotnet build -c Release %~dp0\..\..
call %~dp0\start_server.cmd
@pushd %~dp0\..\..\bin\Release\net8.0
del *.log
del logs\*.log
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -Username player -LocalPort 9000
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -Username player -LocalPort 9001
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -Username player -LocalPort 9002
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -Username player -LocalPort 9003
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -Username player -LocalPort 9004
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -Username player -LocalPort 9005
@popd
