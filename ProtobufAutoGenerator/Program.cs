using System;
using System.Collections.Generic;
using System.IO;
using ProtobufAutoGenerator.Core;
using ProtobufAutoGenerator.Client;

namespace ProtobufAutoGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG_YXM
            if (args.Length == 0)
            {
                args = new string[2];
                args[0] = "E:/Workspace/A1/Server/game/game-server/msg_template/game/";
                args[1] = "E:/Workspace/A1/Client/Assets/AlphaVersion/Scripts/NetManager/Network/";
            }
#endif

            if (args.Length != 2)
            {
                Console.WriteLine("Usage: Program <proto_dir> <output_dir>");
                return;
            }

            string protoDir = args[0];
            string outputDir = args[1];

            if (!Directory.Exists(protoDir))
            {
                Console.WriteLine("<proto_dir> not exists: {0}", protoDir);
                return;
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            ProcessProtos(protoDir, outputDir);
            //Console.Read();
        }

        static readonly string MSG_TYPE_PROTO = "gameMessageTypeMsg.proto";

        static void ProcessProtos(string protoDir, string outputDir)
        {
            var files = Directory.EnumerateFiles(protoDir, "*.proto", SearchOption.TopDirectoryOnly);

            List<ProtoParser> protoList = new List<ProtoParser>();
            GameMessageType gmt = new GameMessageType();
            ProtoMsg protoMsg = new ProtoMsg();
            protoMsg.SetMsgType(gmt);

            Console.WriteLine("Load " + MSG_TYPE_PROTO);
            gmt.LoadProtoFile(protoDir + MSG_TYPE_PROTO);
            Console.WriteLine();

            foreach (string file in files)
            {
                if (file.EndsWith(MSG_TYPE_PROTO))
                    continue;

                Console.WriteLine("Process {0}", file.Substring(file.LastIndexOf('/') + 1));

                ProtoParser proto = new ProtoParser();
                proto.Parse(file);

                string outFile = string.Format("{0}/ProtoMsg/{1}.cs", outputDir, Utility.ToUpperCaseFirst(proto.GetName()));
                if (File.Exists(outFile))
                {
                    CsParser csParser = new CsParser();
                    csParser.Parse(outFile);

                    ProtoMsgMerger merger = new ProtoMsgMerger(proto, csParser);
                    merger.SetMsgType(gmt);
                    merger.WriteToFile(outFile);

                    Console.WriteLine("-- Write to {0}", outFile);
                    Console.WriteLine();
                }
                else
                {
                    protoMsg.SetProto(proto);
                    protoMsg.WriteToFile(outFile);
                    Console.WriteLine("-- Write to {0}", outFile);
                    Console.WriteLine();
                }

                protoList.Add(proto);
            }

            Console.WriteLine("Write DelegateRegister.cs");
            using (var register = new DelegateRegister(gmt))
            {
                register.WriteToFile(string.Format("{0}DelegateRegister.cs", outputDir));
            }
            Console.WriteLine();

            Console.WriteLine("Write MsgResponse.cs");
            using (var msgResponce = new MsgResponse(gmt, protoList))
            {
                msgResponce.WriteToFile(string.Format("{0}MsgResponse.cs", outputDir));
            }
            Console.WriteLine();

            Console.WriteLine("done!");
        }
    }
}
