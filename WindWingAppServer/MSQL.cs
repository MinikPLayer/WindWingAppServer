using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace WindWingAppServer
{
    public class MSQL
    {
        MySqlConnection mySQL;

        string databaseName;
        public MSQL(string host, string username, string password, string database)
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Password = password;
            builder.Server = host;
            builder.UserID = username;
            builder.Database = databaseName = database;

            mySQL = new MySqlConnection(builder.ConnectionString);
            mySQL.Open();
        }

        /// <summary>
        /// Get data from table
        /// </summary>
        /// <param name="table">Table name</param>
        /// <param name="rows">Rows to get, "*" for all and "row1,row2" format for anything else</param>
        /// <param name="args">Additional data, example "WHERE table_schema=test AND (...)"</param>
        /// <param name="limit">Limit results count</param>
        /// <returns></returns>
        public string[] GetData(string table, string rows = "*", string args = "", int limit = -1, char separator = ';')
        {
            string cmd = "SELECT " + rows + " FROM " + table + " " + args;
            if(limit > 0)
            {
                cmd += " LIMIT " + limit.ToString();
            }

            cmd += ';';
            Console.WriteLine("Command: " + cmd);

            return ExecuteCommand(cmd, separator);
        }

        public string[] ExecuteCommand(string cmd, char separator = ';')
        {
            List<string> data = new List<string>();
            using (var command = new MySqlCommand(cmd, mySQL))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string str = "";
                        Debug.Log("Field count: " + reader.FieldCount);
                        //Console.WriteLine("Table: " + reader.GetString(0));
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            str += reader.GetString(i) + separator;
                        }
                        if (str.Length != 0)
                        {
                            str = str.Substring(0, str.Length - 1); // remove last separator
                        }

                        data.Add(str);
                    }
                }
            }

            return data.ToArray();
        }


        public abstract class ColumnType
        {
            public Type type;
            public string sqlTypeStr;

            public ColumnType(Type type, string sqlTypeStr)
            {
                this.sqlTypeStr = sqlTypeStr;
                this.type = type;
            }

            public abstract object GetValue(MySqlDataReader reader, int index); 
            public T GetValue<T>(MySqlDataReader reader, int index)
            {
                if(type != typeof(T))
                {
                    Debug.LogError("[ColumnType.GetValue] Types missmatch");
                    return default;
                }

                return (T)GetValue(reader, index);
            }

            public static ColumnString STRING = new ColumnString();
        }

        public class ColumnString : ColumnType
        {
            public ColumnString()
                :base(typeof(string), "TEXT")
            {

            }

            public override object GetValue(MySqlDataReader reader, int index)
            {
                return reader.GetString(index);
            }
        }

        public class Column
        {
            public string name;
            public ColumnType type;

            public Column(string name, ColumnType type)
            {
                this.name = name;
                this.type = type;
            }
        }

        public void CreateTable(string name, List<Column> columns)
        {
            string cmd = "CREATE TABLE " + name + " (";
            for (int i = 0;i<columns.Count;i++)
            {
                cmd += columns[i].name + " " + columns[i].type.sqlTypeStr + " NOT NULL";
                if (i != columns.Count - 1)
                {
                    cmd += ',';
                }
                else
                {
                    cmd += ");";
                }
            }

            ExecuteCommand(cmd);
        }

        public void DropTable(string name)
        {
            ExecuteCommand("DROP TABLE " + name + ";");
        }

        public bool TableExists(string table)
        {
            string[] result = GetData("information_schema.tables", "TABLE_NAME", "WHERE table_schema = \'" + databaseName + "\' AND table_name = \'" + table + "\'", 1);
            return result.Length == 1;
        }

        ~MSQL()
        {
            if(mySQL != null)
            {
                mySQL.Close();
            }
        }
    }
}
