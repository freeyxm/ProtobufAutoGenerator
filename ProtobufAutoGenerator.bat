@echo off
SET PROTO_PATH=../../../Server/game/game-server/msg_template/game/
SET OUTPUT_PATH=../../../Client/Assets/AlphaVersion/Scripts/NetManager/Network/

echo 1. ���¹��� ...
svn update --accept postpone

echo.
echo 2. ִ�з����Э�����ɹ��� ...
set curDir=%cd%
cd ../../../Server/game/game-server/msg_template/
call gen_all.bat
cd %curDir%

echo.
echo 3.  ִ�пͻ���Э�����ɹ��� ...
call ProtobufAutoGenerator.exe %PROTO_PATH% %OUTPUT_PATH%

pause