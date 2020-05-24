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

            bool clear = false;
            if(args.Length > 0 && args[0] == "-clear")
            {
                clear = true;
                Debug.Log("Clearing DB...");
            }

            WindWingAppServer server = new WindWingAppServer(clear);
        }
    }
}
