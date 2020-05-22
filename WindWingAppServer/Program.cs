using System;
using MySql.Data.MySqlClient;
using MySqlConnector;

namespace WindWingAppServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            /*MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Password = "Misiek200111";
            builder.Server = "localhost";
            builder.UserID = "Minik";
            builder.Database = "testdb";

            using (var connection = new MySqlConnection(builder.ConnectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand("SHOW TABLES;", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine("Table: " + reader.GetString(0));
                        }
                    }
                }

                using (var command = new MySqlCommand("SELECT login,password,email FROM users;", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine("Login: " + reader.GetString(0));
                            Console.WriteLine("Password: " + reader.GetString(1));
                            Console.WriteLine("E-Mail: " + reader.GetString(2));
                        }
                    }
                }
            }*/

            MSQL sql = new MSQL("localhost", "Minik", "Misiek200111", "testDB");

            bool exists = sql.TableExists("users");
            Console.WriteLine("Table testdb exists: " + exists);
            if(exists)
            {
                sql.DropTable("users");
                sql.CreateTable("users", new System.Collections.Generic.List<MSQL.Column>() { new MSQL.Column("login", MSQL.ColumnType.STRING), new MSQL.Column("password", MSQL.ColumnType.STRING), new MSQL.Column("email", MSQL.ColumnType.STRING) });
            }
            else
            {
                sql.CreateTable("users", new System.Collections.Generic.List<MSQL.Column>() { new MSQL.Column("login", MSQL.ColumnType.STRING), new MSQL.Column("password", MSQL.ColumnType.STRING), new MSQL.Column("email", MSQL.ColumnType.STRING) });

            }



        }
    }
}
