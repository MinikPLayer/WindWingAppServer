using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;

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

            return ExecuteCommand(cmd, separator);
        }


        public string lastError = "";
        public bool error = false;
        bool sqlBusy = false;
        public string[] ExecuteCommand(string cmd, char separator = ';', int level = 0)
        {
            while(sqlBusy)
            {
                Thread.Sleep(10);
            }

            List<string> data = new List<string>();
            sqlBusy = true;
            try
            {
                
                if(mySQL.State != System.Data.ConnectionState.Open)
                {
                    Debug.LogWarning("SQL connection is not opened, reopening");
                    mySQL.Close();
                    mySQL.Open();
                    error = false;
                }

                using (var command = new MySqlCommand(cmd, mySQL))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string str = "";
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
            }
            catch(System.IO.IOException e)
            {
                if(level > 0)
                {
                    Debug.Exception(e, "[MSQL.ExecuteCommand.IOException] Command: \"" + cmd + "\"");
                    error = true;
                    lastError = e.ToString();
                }
                else
                {
                    return ExecuteCommand(cmd, separator, level + 1);
                }
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[MSQL.ExecuteCommand] Command: \"" + cmd + "\"");
                error = true;
                lastError = e.ToString();
            }

            sqlBusy = false;
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
                if(type != typeof(T) && typeof(T) != typeof(object))
                {
                    Debug.LogError("[ColumnType.GetValue] Types missmatch");
                    return default;
                }

                try
                {
                    return (T)GetValue(reader, index);
                }
                catch(InvalidCastException e)
                {
                    Debug.LogWarning("Entry is null");
                    return default;
                }
                catch(Exception e)
                {
                    Debug.Exception(e, "[MSQL.GetValue<T>]");
                    return default;
                }
            }

            public static ColumnString STRING = new ColumnString();
            public static ColumnInt INT = new ColumnInt();
            public static ColumnBool BOOLEAN = new ColumnBool();
            public static ColumnDateTime DATETIME = new ColumnDateTime();
            public static ColumnTime TIME = new ColumnTime();
        }

        public class ColumnString : ColumnType { 
            public ColumnString() :base(typeof(string), "TEXT") { }
            public override object GetValue(MySqlDataReader reader, int index) { return reader.GetString(index); }
        }

        public class ColumnInt : ColumnType { 
            public ColumnInt() : base(typeof(int), "INT") { }
            public override object GetValue(MySqlDataReader reader, int index){ return reader.GetInt32(index); }
        }

        public class ColumnBool : ColumnType {
            public ColumnBool() : base(typeof(bool), "BOOLEAN") { }
            public override object GetValue(MySqlDataReader reader, int index) { return reader.GetBoolean(index); }
        }

        public class ColumnDateTime : ColumnType { 
            public ColumnDateTime() : base(typeof(bool), "DATETIME") { }
            public override object GetValue(MySqlDataReader reader, int index) { return reader.GetDateTime(index); }
        }

        public class ColumnTime : ColumnType
        {
            public ColumnTime() : base(typeof(bool), "INT") { }
            public override object GetValue(MySqlDataReader reader, int index) 
            {
                //return reader.GetTimeSpan(index); 
                return TimeSpan.FromMilliseconds(reader.GetInt32(index));
            }
        }

        public class Column
        {
            public string name;
            public ColumnType type;
            public bool notNull;

            public Column(string name, ColumnType type, bool notNull = true)
            {
                this.name = name;
                this.type = type;
                this.notNull = notNull;
            }
        }

        public void CopyTable(string name, string newName)
        {
            ExecuteCommand("CREATE TABLE " + newName + " LIKE " + name + ";INSERT INTO " + newName + " SELECT * FROM " + name + ";");
        }

        public void CreateTable(string name, List<Column> columns)
        {
            string cmd = "CREATE TABLE " + name + " (";
            for (int i = 0;i<columns.Count;i++)
            {
                cmd += columns[i].name.Replace("\'", "\'\'") + " " + columns[i].type.sqlTypeStr;
                if(columns[i].notNull)
                {
                    cmd += " NOT NULL";
                }
                if (i != columns.Count - 1)
                {
                    cmd += ',';
                }
                else
                {
                    cmd += ") CHARSET=utf32 COLLATE utf32_bin;";
                }
            }

            ExecuteCommand(cmd);
        }

        public void DropTable(string name)
        {
            ExecuteCommand("DROP TABLE " + name + ";");
        }

        string[] GetTablesNames()
        {
            return GetData("information_schema.tables", "TABLE_NAME", "WHERE table_schema = \'" + databaseName + "\'");
        }

        public void DropAllTables()
        {
            string[] names = GetTablesNames();
            for(int i = 0;i<names.Length;i++)
            {
                DropTable(names[i]);
            }
        }

        public bool TableExists(string table)
        {
            string[] result = GetData("information_schema.tables", "TABLE_NAME", "WHERE table_schema = \'" + databaseName + "\' AND table_name = \'" + table + "\'", 1);
            return result.Length == 1;
        }

        public class Value
        {
            public string name;
            public string value;

            public Value(string name, string value)
            {
                this.name = name;
                if (value == null) value = "";
                this.value = value;
            }

            public Value(string name, int value)
            {
                this.name = name;
                this.value = value.ToString();
            }
            public Value(string name, bool value)
            {
                this.name = name;
                if (value)
                {
                    this.value = "1";
                }
                else
                {
                    this.value = "0";
                }
            }

            public Value(string name, DateTime value)
            {
                this.name = name;
                this.value = value.Year + "-" + value.Month + "-" + value.Day + " " + value.Hour + ":" + value.Minute + ":" + value.Second;
            }

            public Value(string name, TimeSpan value)
            {
                this.name = name;
                this.value = ((int)value.TotalMilliseconds).ToString();
            }
        }

        public void AddEntries(string table, List<string> names, List<List<string>> values)
        {
            for(int i = 0;i<values.Count;i++)
            {
                if(values[i].Count > names.Count)
                {
                    Debug.LogError("[MSQL.AddEntries] Not enough names");
                    return;
                }
                List<Value> vs = new List<Value>();
                for(int j = 0;j<values[i].Count;j++)
                {
                    vs.Add(new Value(names[j].Replace("\'", "\'\'"), values[i][j].Replace("\'", "\'\'")));
                }

                AddEntry(table, vs);
            }
        }

        public bool AddEntry(string table, List<Value> values)
        {
            for(int i = 0;i<values.Count;i++)
            {
                if(values[i].value.Length == 0)
                {
                    values.RemoveAt(i);
                    i--;
                    break;
                }
            }

            string cmd = "INSERT INTO " + table + "(";
            for(int i = 0;i<values.Count;i++)
            {
                cmd += values[i].name.Replace("\'", "\'\'");
                if (i != values.Count - 1)
                {
                    cmd += ",";
                }
                else
                {
                    cmd += ") VALUES (\'";
                }
            }
            for (int i = 0; i < values.Count; i++)
            {
                cmd += values[i].value.Replace("\'", "\'\'");
                if (i != values.Count - 1)
                {
                    cmd += "\',\'";
                }
                else
                {
                    cmd += "\');";
                }
            }

            ExecuteCommand(cmd);
            return !error;
        }

        public bool RemoveEntry(string table, Value value)
        {
            ExecuteCommand("DELETE FROM " + table + " WHERE " + value.name.Replace("\'", "\'\'") + " = " + value.value.Replace("\'", "\'\'"));
            return !error;
        }

        public void ModifyEntry(string table, Value value, string where = "")
        {
            if(where.Length > 0)
            {
                where = where.Insert(0, " WHERE ");
            }
            ExecuteCommand("UPDATE " + table + " SET " + value.name.Replace("\'", "\'\'") + " = " + value.value.Replace("\'", "\'\'") + where + ";");
        }

        public void ModifyEntries(string table, List<Value> values, string where = "")
        {
            string cmd = "UPDATE " + table + " SET ";
            for(int i = 0;i<values.Count;i++)
            {
                cmd += values[i].name.Replace("\'", "\'\'") + " = " + "\'" + values[i].value.Replace("\'", "\'\'") + "\'";
                if(i == values.Count - 1)
                {
                    if(where.Length > 0)
                    {
                        cmd += " WHERE " + where;
                    }
                    cmd += ';';
                }
                else
                {
                    cmd += ',';
                }
            }

            ExecuteCommand(cmd);
        }

        public T[] ReadEntry<T>(string table, Column column, string where = "", int level = 0)
        {
            string cmd = "SELECT " + column.name + " FROM " + table + " " + where;
            try
            {

                if (mySQL.State != System.Data.ConnectionState.Open)
                {
                    Debug.LogWarning("SQL connection is not opened, reopening");
                    mySQL.Close();
                    mySQL.Open();
                    error = false;
                }

                
                List<T> objects = new List<T>();
                using (var command = new MySqlCommand(cmd, mySQL))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            objects.Add(column.type.GetValue<T>(reader, 0));
                        }
                    }
                }

                return objects.ToArray();
            }
            catch (System.IO.IOException e)
            {
                if (level > 0)
                {
                    Debug.Exception(e, "[MSQL.ReadEntry.IOException] Command: \"" + cmd + "\"");
                    error = true;
                    lastError = e.ToString();
                }
                else
                {
                    return ReadEntry<T>(table, column, where, level + 1);
                }
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[MSQL.ReadEntry] Command: \"" + cmd + "\"");
                error = true;
                lastError = e.ToString();

                
            }
            return default;
        }

        public object[] ReadEntry(string table, Column column, string where = "")
        {
            /*string cmd = "SELECT " + column.name + " FROM " + table + " " + where;
            List<object> objects = new List<object>();
            using (var command = new MySqlCommand(cmd, mySQL))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        objects.Add(column.type.GetValue(reader, 0));
                    }
                }
            }

            return objects.ToArray();*/

            return ReadEntry<object>(table, column, where);
        }

        public List<object[]> ReadEntries(string table, List<Column> columns, string where = "")
        {

            List<object[]> arrays = new List<object[]>();
            for(int i = 0;i<columns.Count;i++)
            {
                arrays.Add(ReadEntry(table, columns[i], where));
            }
            return arrays;

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
