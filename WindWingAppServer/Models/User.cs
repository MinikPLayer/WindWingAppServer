using System;
using System.Collections.Generic;
using System.Text;

namespace WindWingAppServer.Models
{
    public class User
    {
        public int id;
        public string login;
        public string password;
        public string token;
        public string email;
        public string steam;
        public string ip;

        public bool newUserType;

        public bool admin = false;

        public Network.Connection connection = null;

        public bool good = true;

        public int donate = 0;
        public void LoadDefaults()
        {
            this.id = 0;
            this.login = "";
            this.steam = "";
        }


        public User()
        {

        }

        public User(string serialized)
        {
            LoadDefaults();

            good = Deserialize(serialized);
        }

        public User(int id, string login, string password, string token, string email, string steam, string ip, bool admin = false)
        {
            FillVariables(id, login, password, token, email, steam, ip, admin);
        }

        public void FillVariables(int id, string login, string password, string token, string email, string steam, string ip, bool admin = false)
        {
            this.id = id;
            this.login = login;
            this.password = password;
            this.steam = steam;

            this.admin = admin;
            this.ip = ip;
            this.email = email;
            this.token = token;

            newUserType = true;
        }

        bool ParseSinglePacket(string header, string content)
        {
            try
            {
                switch (header)
                {
                    case "id":
                        this.id = int.Parse(content);
                        return true;

                    case "login":
                        this.login = content;
                        return true;

                    case "steam":
                        this.steam = content;
                        return true;

                    default:
                        Debug.LogError("[Season.SeasonUser.ParseSinglePacket] Unknown header");
                        return false;
                }
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[Season.SeasonUser.ParseSinglePacket]");
                return false;
            }
        }

        public bool Deserialize(string str)
        {
            try
            {
                if (!str.StartsWith("user{"))
                {
                    Debug.LogError("[Season.SeasonUser.Deserialize] It's no a race packet, bad magic");
                    return false;
                }

                str = str.Substring(0, str.Length - 1).Remove(0, 5); // remove race{ and }

                List<string> packets = MUtil.SplitWithBrackets(str);
                for (int i = 0; i < packets.Count; i++)
                {
                    bool done = false;
                    for (int j = 0; j < packets[i].Length; j++)
                    {
                        if (packets[i][j] == '{')
                        {
                            if (!ParseSinglePacket(packets[i].Substring(0, j), packets[i].Substring(0, packets[i].Length - 1).Remove(0, j + 1)))
                            {
                                Debug.LogError("[Season.SeasonUser.Deserialize] Cannot parse packet with header: " + packets[i].Substring(0, j));
                                return false;
                            }
                            done = true;
                            break;
                        }
                    }
                    if (!done)
                    {
                        Debug.LogError("[Season.SeasonUser.Deserialize] Cannot find a closing bracket, incomplete packet");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[Season.SeasonUser.Deserialize]");
                return false;
            }
        }

        public string Serialize()
        {
            return "user{id{" + id.ToString() + "},login{" + login + "},steam{" + steam + "}}";
        }

        public List<string> ToSQL()
        {
            return new List<string>() { id.ToString(), login, password, token, email, steam, admin ? "1" : "0", donate.ToString() };
        }

        public List<MSQL.Value> ToSqlValues()
        {
            return new List<MSQL.Value>() {
                    new MSQL.Value("id", id),
                    new MSQL.Value("login", login),
                    new MSQL.Value("password", password),
                    new MSQL.Value("token", token),
                    new MSQL.Value("email", email),
                    new MSQL.Value("steam", steam),
                    new MSQL.Value("ip", ip),
                    new MSQL.Value("admin", admin),
                    new MSQL.Value("donate", donate)
            };
        }



        public bool LoadFromSql(object[] data)
        {
            try
            {
                if (data.Length < 9)
                {
                    Debug.LogError("[User.LoadFromSqlUsers] Not enough data to load from, found only " + data.Length + " columns");
                    return false;
                }

                id = (int)data[0];
                login = (string)data[1];
                password = (string)data[2];
                token = (string)data[3];
                email = (string)data[4];
                steam = (string)data[5];
                ip = (string)data[6];
                admin = (bool)data[7];
                donate = (int)data[8];

                return true;
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[User.LoadFromSql]");
                return false;
            }
        }

        public static User GetUser(int id)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].id == id)
                {
                    return users[i];
                }
            }

            return null;

        }

        public static List<User> users = new List<User>();
    }
}
