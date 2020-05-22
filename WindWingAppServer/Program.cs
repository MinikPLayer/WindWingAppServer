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

            WindWingAppServer server = new WindWingAppServer();
        }
    }
}
