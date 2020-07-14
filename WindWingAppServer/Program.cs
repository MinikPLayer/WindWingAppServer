using System;
using MySql.Data.MySqlClient;
using MySqlConnector;

using System.Collections.Generic;

namespace WindWingAppServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");

            string add = "";

            bool clear = false;
            if(args.Length > 0 && args[0] == "-clear")
            {
                clear = true;
                Debug.Log("Clearing DB...");
            }
            if(args.Length > 0 && args[0] == "-addDNF")
            {
                Debug.Log("Adding DNF to tables");
                add = "-addDNF";
            }

            #if !DEBUG
                if (clear)
                {
                    clear = MUtil.AskUserYesNo("clear the database?");
                }
            #endif

            WindWingAppServer server = new WindWingAppServer(clear, add);
        }
    }
}
