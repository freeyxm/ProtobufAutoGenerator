@echo off
SET PROTO_PATH=../../../Server/game/game-server/msg_template/game/
SET OUTPUT_PATH=../../../Client/Assets/AlphaVersion/Scripts/NetManager/Network/
call ProtobufAutoGenerator.exe %PROTO_PATH% %OUTPUT_PATH%

pause