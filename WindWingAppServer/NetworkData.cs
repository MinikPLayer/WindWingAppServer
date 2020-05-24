using System;
using System.Collections.Generic;
using System.Text;

using Network;
using Network.Converter;
using Network.Packets;

namespace WindWingAppServer
{
    public class NetworkData
    {
        List<WindWingAppServer.User> loggedIn = new List<WindWingAppServer.User>();

        short port;
        WindWingAppServer server;

        WindWingAppServer.User GetUser(Connection con)
        {
            for(int i = 0;i<loggedIn.Count;i++)
            {
                if(loggedIn[i].connection == con)
                {
                    return loggedIn[i];
                }
            }

            return null;
        }

        public NetworkData(WindWingAppServer server, short port)
        {
            this.server = server;
            this.port = port;

            ServerConnectionContainer container = ConnectionFactory.CreateServerConnectionContainer(port, false);
            container.AllowUDPConnections = false;
            container.ConnectionEstablished += Container_ConnectionEstablished;
            container.ConnectionLost += Container_ConnectionLost;
            container.StartTCPListener();
        }

        private void Container_ConnectionLost(Connection connection, Network.Enums.ConnectionType connectionType, Network.Enums.CloseReason closeReason)
        {
            Debug.Log($"Connection lost with {connection.IPRemoteEndPoint}, connection type {connectionType}, reason: {closeReason}");
        
            for(int i = 0;i<loggedIn.Count;i++)
            {
                if(loggedIn[i].connection == connection)
                {
                    loggedIn[i].connection = null;
                    loggedIn.RemoveAt(i);
                    break; 
                }
            }
        }

        private void Container_ConnectionEstablished(Connection connection, Network.Enums.ConnectionType connectionType)
        {
            Debug.Log($"Connection established with {connection.IPRemoteEndPoint}, type: {connectionType}");

            //connection.RegisterStaticPacketHandler<DataRequest>(DataRequested);
            connection.RegisterRawDataHandler("Leaderboards", LeaderboardsDataRequested);
            connection.RegisterRawDataHandler("Login", LoginRequest);
            connection.RegisterRawDataHandler("LoginT", LoginTRequest);
            connection.RegisterRawDataHandler("Info", InfoRequest);
            connection.RegisterRawDataHandler("RegisterSeason", RegisterToSeasonRequest);
            connection.RegisterRawDataHandler("RegisterUser", RegisterUserRequest);
        }

        string GetString(RawData data)
        {
            return RawDataConverter.ToUTF16_LittleEndian_String(data);
        }
        
        RawData GetRawData(string key, string data)
        {
            return RawDataConverter.FromUTF16_LittleEndian_String(key, data);
        }

        void Send(Connection connection, string key, string data)
        {
            connection.SendRawData(GetRawData(key, data));
        }

        // WARNINR - PLACEHOLDERS
        void InfoRequest(RawData data, Connection connection)
        {
            string[] info = GetString(data).Split(';');
            if(info.Length == 0)
            {
                Send(connection, data.Key, "ER;Empty Request");
                return;
            }

            switch (info[0])
            {
                case "SC": // Seasons Count
                    Send(connection, data.Key, server.seasons.Count.ToString());
                    break;

                case "RD": // Registration Data
                    WindWingAppServer.RegistrationData[] datas = server.GetRegistrationData();

                    if(datas.Length == 0)
                    {
                        Send(connection, data.Key, "0");
                        return;
                    }

                    string response = datas.Length.ToString() + "{";
                    for(int i = 0;i<datas.Length;i++)
                    {
                        response += datas[i].ToString();
                    }
                    response += "}";
                    Send(connection, data.Key, response);
                    break;
                default:
                    Send(connection, data.Key, "UR;Unknown request");
                    break;
            }

        }

        void RegisterUserRequest(RawData data, Connection con)
        {
            try
            {
                string[] info = GetString(data).Split(';');
                if(info.Length < 4) // login, password, email, steam
                {
                    Send(con, data.Key, "ND;Zbyt malo informacji");
                    return;
                }

                info[1] = info[1].Replace("\\:", ";");

                // Check if email contains @ and .
                if(!info[2].Contains('@') || !info[2].Contains('.'))
                {
                    Send(con, data.Key, "BE;Adres e-mail jest niepoprawny");
                    Debug.Log("Bad format e-mail address: " + info[2]);
                    return;
                }


                WindWingAppServer.User user = server.GetUser(info[0]);
                if (user != null)
                {
                    Send(con, data.Key, "UE;Uzytkownik o takiej nazwie juz istnieje");
                    Debug.Log("User already exists");
                    return;
                }

                user = server.GetUserByMail(info[2]);
                if (user != null)
                {
                    Send(con, data.Key, "UE;Uzytkownik o takim e-mail juz istnieje");
                    Debug.Log("User with that email account already exists");
                    return;
                }

                user = server.GetUserBySteam(info[3]);
                if (user != null)
                {
                    Send(con, data.Key, "UE;Uzytkownik o takim koncie steam juz istnieje");
                    Debug.Log("User with that steam account already exists");
                    return;
                }

                // Adres email
                info[2] = info[2].ToLower();

                user = server.CreateUser(info[0], info[1], info[2], info[3], con.IPRemoteEndPoint.Address.ToString());
                server.RegisterUser(user);

                Send(con, data.Key, "OK;" + user.token.Replace(";", "\\:"));
            }
            catch(Exception e)
            {
                Send(con, data.Key, "SC;Blad przetwarzania wniosku;" + e.ToString());
            }
        }

        void RegisterToSeasonRequest(RawData data, Connection con)
        {
            try
            {
                WindWingAppServer.User user = GetUser(con);
                if(user == null)
                {
                    Send(con, data.Key, "NL;Uzytkownik nie jest zalogowany");
                    Debug.Log("User not logged in");
                    return;
                }

                string[] info = GetString(data).Split(';');
                // Data structure: season number, time dry, time wet, time dry link, time wet link
                if (info.Length < 5)
                {
                    Send(con, data.Key, "ND;Zbyt malo informacji");
                    return;
                }

                int seasonNmbr = int.Parse(info[0]);

                WindWingAppServer.Season season = server.GetSeason(seasonNmbr);
                if(season == null)
                {
                    Send(con, data.Key, "BS;Bledny numer sezonu");
                    return;
                }


                
            }
            catch(Exception e)
            {
                Send(con, data.Key, "SC;Blad przetwarzania wniosku;" + e.ToString());
            }
        }

        void LoginTRequest(RawData data, Connection con)
        {
            try
            {
                string[] info = GetString(data).Split(';');
                if (info.Length < 2)
                {
                    //connection.SendRawData(GetRawData(data.Key, "BI"));
                    Debug.Log("Not enough information specified");
                    Send(con, data.Key, "BP;Nie sprecyzowano wszystkich informacji");
                    return;
                }

                info[1] = info[1].Replace("\\:", ";");

                WindWingAppServer.User user = server.GetUserByToken(info[0], info[1], false);
                if (user == null)
                {
                    Debug.Log("Bad credentials");
                    Send(con, data.Key, "BC;Bledny login lub token, zaloguj sie ponownie");
                    return;
                }

                user.connection = con;
                loggedIn.Add(user);

                Debug.Log("LoginT ok, sending: \"OK;" + user.login + "\"");
                Send(con, data.Key, "OK;" + user.login + ";" + user.admin);
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[NetworkData.LoginRequest] has crashed");
                Send(con, data.Key, "SC;Internal server error when processing the request\n" + e.ToString());
            }
        }

        void LoginRequest(RawData data, Connection con)
        {
            try
            {
                string[] info = GetString(data).Split(';');
                if (info.Length < 2)
                {
                    //connection.SendRawData(GetRawData(data.Key, "BI"));
                    Debug.Log("Not enough information specified");
                    Send(con, data.Key, "BP;Nie sprecyzowano wszystkich informacji");
                    return;
                }

                info[1] = info[1].Replace("\\:", ";");

                WindWingAppServer.User user = server.GetUser(info[0], info[1], false);
                if (user == null)
                {
                    Debug.Log("Bad credentials");
                    Send(con, data.Key, "BC;Bledny login lub haslo");
                    return;
                }

                user.connection = con;
                loggedIn.Add(user);

                Debug.Log("Login ok, sending: \"OK;" + user.login + "\"");
                Send(con, data.Key, "OK;" + user.login + ";" + user.token.Replace(";", "\\:") + ";" + user.admin);
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[NetworkData.LoginRequest] has crashed");
                Send(con, data.Key, "SC;Internal server error when processing the request\n" + e.ToString());
            }
        }

        void LeaderboardsDataRequested(RawData data, Connection con)
        {
            Send(con, data.Key, server.GenerateLeaderboardsString());
            //con.SendRawData(RawDataConverter.FromUTF8String("Leaderboards", server.GenerateLeaderboardsString()));
        }
    }
}
