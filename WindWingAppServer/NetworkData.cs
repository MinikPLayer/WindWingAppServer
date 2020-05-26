using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Network;
using Network.Converter;
using Network.Packets;

using WindWingAppServer.Models;

namespace WindWingAppServer
{
    public class NetworkData
    {
        List<User> loggedIn = new List<User>();

        short port;
        WindWingAppServer server;

        User GetUser(Connection con)
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

            try
            {
                switch (info[0])
                {
                    case "UD": // User data
                        {
                            if (info.Length < 2)
                            {
                                Send(connection, data.Key, "BP;Brak wystarczajacej ilosci parametrow");
                                return;
                            }
                            User user = GetUser(connection);
                            if (user == null)
                            {
                                Send(connection, data.Key, "NL;Musisz być zalogowanym aby otrzymać takie informacje");
                                return;
                            }

                            int userID = int.Parse(info[1]);

                            User target = User.GetUser(userID);

                            Send(connection, data.Key, "OK;" + target.Serialize());
                            break;
                        }

                    case "SC": // Seasons Count
                        Send(connection, data.Key, server.seasons.Count.ToString());
                        break;

                    case "RD": // Registration Data
                        RegistrationData[] datas = server.GetRegistrationData();

                        if (datas.Length == 0)
                        {
                            Send(connection, data.Key, "0");
                            return;
                        }

                        string response = datas.Length.ToString() + "{";
                        for (int i = 0; i < datas.Length; i++)
                        {
                            response += datas[i].ToString();
                        }
                        response += "}";
                        Send(connection, data.Key, response);
                        break;

                    case "SD": // Season data
                        {
                            if (info.Length < 2)
                            {
                                Send(connection, data.Key, "BP;Brak wystarczajacej ilosci parametrow");
                                return;
                            }

                            User user = GetUser(connection);

                            int seasonN = int.Parse(info[1]);
                            var season = server.GetSeason(seasonN);
                            if (season == null)
                            {
                                Send(connection, data.Key, "BS;Nie znaleziono sezonu o tym numerze");
                                return;
                            }

                            //string str = "(" + season.id.ToString() + "," + season.racesCount + "," + season.users.Count + ")";
                            string str = season.Serialize();

                            bool found = false;
                            if (user != null)
                            {
                                for (int i = 0; i < season.users.Count; i++)
                                {
                                    if (season.users[i].user == user)
                                    {
                                        found = true;
                                        str += ";True";
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    str += ";False";
                                }
                            }

                            Send(connection, data.Key, str);
                            break;
                        }

                    default:
                        Send(connection, data.Key, "UR;Nieznany tag info");
                        break;
                }

            }
            catch(Exception e)
            {
                Send(connection, data.Key, "IE;Blad wewnetrzny servera;" + e.ToString());
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


                User user = server.GetUser(info[0]);
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
                User user = GetUser(con);
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

                Season season = server.GetSeason(seasonNmbr);
                if(season == null)
                {
                    Send(con, data.Key, "BS;Bledny numer sezonu");
                    return;
                }

                if(!season.registrationData.opened)
                {
                    Send(con, data.Key, "RC;Rejestracja do tego sezonu została już zamknięta");
                    return;
                }

                var sUser = server.AddSeasonUser(season, user, TimeSpan.Parse(info[1]), TimeSpan.Parse(info[2]), info[3], info[4]);

                if(sUser == null)
                {
                    Send(con, data.Key, "IE;Błąd dodawania użytkownika, użytkownik prawdopodobnie już został zarejestrowany w tym sezonie");
                    return;
                }

                Send(con, data.Key, "OK");
            }
            catch(Exception e)
            {
                Send(con, data.Key, "SC;Blad przetwarzania wniosku;" + e.ToString());
            }
        }

        void AdminRequest(RawData data, Connection con)
        {
            try
            {
                var user = GetUser(con);
                if (!user.admin)
                {
                    Send(con, data.Key, "NA;Nie jestes adminem, przykro mi");
                    return;
                }

                string[] info = GetString(data).Split(';');

                if (info.Length == 0)
                {
                    Send(con, data.Key, "EM;Pusta wiadomosc");
                    return;
                }

                switch (info[0])
                {
                    // 0: Season, 1: Add / Modify / Remove, 2: seasonID, {rest}
                    case "Season":

                        if (info.Length < 3)
                        {
                            Send(con, data.Key, "ND;Zbyt malo informacji");
                            return;
                        }

                        

                        switch (info[1])
                        {
                            case "Add":
                                // 0: Season, 1: Add, 2: serialized season
                                Season newSeason = new Season(info[2]);
                                //newSeason.Log();

                                if(!newSeason.good)
                                {
                                    Send(con, data.Key, "NG;Błąd przetwarzania sezonu");
                                    Debug.Log("Season deserializing error, serialized string: \n" + info[2]);
                                    return;
                                }

                                server.AddSeason(newSeason);
                                Send(con, data.Key, "OK");
                                break;

                            case "Modify":
                                {
                                    int seasonID = int.Parse(info[2]);

                                    if (info.Length < 4)
                                    {
                                        Send(con, data.Key, "ND;Zbyt malo informacji");
                                        return;
                                    }

                                    var season = server.GetSeason(seasonID);
                                    if (season.Deserialize(info[3]))
                                    {
                                        Send(con, data.Key, "OK");
                                        //season.Log();
                                        break;
                                    }
                                    Send(con, data.Key, "PE;Wystąpił błąd podczas przetwarzania żądania");
                                    return;
                                }

                            case "Remove":
                                {
                                    int seasonID = int.Parse(info[2]);

                                    if(!server.RemoveSeason(seasonID))
                                    {
                                        Send(con, data.Key, "UE;Nienzany błąd przy usuwaniu sezonu");
                                        return;
                                    }

                                    Send(con, data.Key, "OK");

                                    return;
                                }

                            default:
                                Send(con, data.Key, "BT;Zły tag");
                                return;
                        }

                        break;

                    default:
                        Send(con, data.Key, "NF;Nie znaleziono tagu");
                        return;
                }
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[NetworkData.AdminRequest]");
                Send(con, data.Key, "IE;Wewnętrzny błąd servera;" + e.ToString());
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

                User user = server.GetUserByToken(info[0], info[1], false);
                if (user == null)
                {
                    Debug.Log("Bad credentials");
                    Send(con, data.Key, "BC;Bledny login lub token, zaloguj sie ponownie");
                    return;
                }

                user.connection = con;
                loggedIn.Add(user);
                Send(con, data.Key, "OK;" + user.login + ";" + user.admin);

                if(user.admin)
                {
                    con.RegisterRawDataHandler("Admin", AdminRequest);
                }
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

                User user = server.GetUser(info[0], info[1], false);
                if (user == null)
                {
                    Debug.Log("Bad credentials");
                    Send(con, data.Key, "BC;Bledny login lub haslo");
                    return;
                }

                user.connection = con;
                loggedIn.Add(user);
                Send(con, data.Key, "OK;" + user.login + ";" + user.token.Replace(";", "\\:") + ";" + user.admin);

                if (user.admin)
                {
                    con.RegisterRawDataHandler("Admin", AdminRequest);
                }
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[NetworkData.LoginRequest] has crashed");
                Send(con, data.Key, "SC;Internal server error when processing the request\n" + e.ToString());
            }
        }

        void LeaderboardsDataRequested(RawData data, Connection con)
        {
            try
            {
                string[] info = GetString(data).Split(';');

                if (info.Length == 0)
                {
                    Debug.Log("Not enough information specified");
                    Send(con, data.Key, "BP;Nie sprecyzowano wszystkich informacji");
                    return;
                }

                Send(con, data.Key, server.GenerateLeaderboardsString(int.Parse(info[0])));
            }
            catch(Exception e)
            {
                Send(con, data.Key, "IE;Wewnętrzny błąd servera podczas przetwarzania żądania;" + e.ToString());
                Debug.Exception(e, "[NetworkData.LeaderboardsDataRequested]");
            }
            //con.SendRawData(RawDataConverter.FromUTF8String("Leaderboards", server.GenerateLeaderboardsString()));
        }
    }
}
