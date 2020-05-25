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

        public User()
        {

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

        public List<string> ToSQL()
        {
            return new List<string>() { id.ToString(), login, password, token, email, steam, admin ? "1" : "0" };
        }

        public List<MSQL.Value> ToSqlValues()
        {
            return new List<MSQL.Value>(){
                    new MSQL.Value("id", id),
                    new MSQL.Value("login", login),
                    new MSQL.Value("password", password),
                    new MSQL.Value("token", token),
                    new MSQL.Value("email", email),
                    new MSQL.Value("steam", steam),
                    new MSQL.Value("ip", ip),
                    new MSQL.Value("admin", admin)
                };
        }



        public void LoadFromSql(object[] data)
        {
            if (data.Length < 5)
            {
                Debug.LogError("[User.LoadFromSqlUsers] Not enough data to load from, found only " + data.Length + " columns");
                return;
            }

            id = (int)data[0];
            login = (string)data[1];
            password = (string)data[2];
            token = (string)data[3];
            email = (string)data[4];
            steam = (string)data[5];
            admin = (bool)data[6];
        }


    }
}
