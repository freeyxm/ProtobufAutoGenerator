@echo off
SET PROTO_PATH=../../../Server/game/game-server/msg_template/game/
SET OUTPUT_PATH=../../../Client/Assets/AlphaVersion/Scripts/NetManager/Network/

echo 1. 更新工具 ...
svn update --accept postpone

echo.
echo 2. 执行服务端协议生成工具 ...
set curDir=%cd%
cd ../../../Server/game/game-server/msg_template/
call gen_all.bat
cd %curDir%

echo.
echo 3.  执行客户端协议生成工具 ...
call ProtobufAutoGenerator.exe %PROTO_PATH% %OUTPUT_PATH%

pause