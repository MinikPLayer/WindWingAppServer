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
            try
            {
                Debug.Log($"Connection established with {connection.IPRemoteEndPoint}");

                connection.RegisterRawDataHandler("WP", WelcomePacketRequest); // Welcome Packet

            }
            catch(Exception e)
            {
                Debug.Exception(e, "[NetworkData.ConnectionEstablished]");
            }
        }

        void WelcomePacketRequest(RawData data, Connection connection)
        {
            try
            {
                

                string[] info = GetString(data).Split(';');
                if (info.Length == 0)
                {
                    Send(connection, data.Key, "ER;Empty Request");
                    return;
                }

                if (info.Length < 1)
                {
                    Send(connection, data.Key, "BP;Brak wystarczajacej ilosci parametrow");
                    return;
                }

                int version = int.Parse(info[0]);

                Debug.Log("Got welcome packet from " + connection.IPRemoteEndPoint.Address.ToString() + " - version: " + version.ToString());

                connection.RegisterRawDataHandler("Leaderboards", LeaderboardsDataRequested);
                connection.RegisterRawDataHandler("Login", LoginRequest);
                connection.RegisterRawDataHandler("LoginT", LoginTRequest);
                connection.RegisterRawDataHandler("Info", InfoRequest);
                connection.RegisterRawDataHandler("RegisterSeason", RegisterToSeasonRequest);
                connection.RegisterRawDataHandler("SeasonUser", SeasonUserRequest);
                connection.RegisterRawDataHandler("RegisterUser", RegisterUserRequest);

                Send(connection, data.Key, "OK");
            }
            catch(Exception e)
            {
                Debug.Exception(e, "Error processing welcome packet ");
                Send(connection, data.Key, "BV;Błędny kod wersji");
            }
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

        
        void SeasonUserRequest(RawData data, Connection con)
        {
            try
            {
                Debug.Log("Season user request");
                User user = GetUser(con);
                if (user == null)
                {
                    Send(con, data.Key, "NL;Uzytkownik nie jest zalogowany");
                    Debug.Log("User not logged in");
                    return;
                }

                string[] info = GetString(data).Split(';');
                if (info.Length == 0)
                {
                    Send(con, data.Key, "ER;Empty Request");
                    return;
                }

                if (info.Length < 3)
                {
                    Send(con, data.Key, "BP;Brak wystarczajacej ilosci parametrow");
                    return;
                }

                var season = server.GetSeason(int.Parse(info[0]));
                if(season == null)
                {
                    Send(con, data.Key, "BS;Nie znaleziono sezonu");
                    return;
                }

                User target = null;
                if (info[2] == "C")
                {
                    target = user;
                }
                else
                {
                    int id = int.Parse(info[2]);
                    target = User.GetUser(id);
                    if(target == null)
                    {
                        Send(con, data.Key, "BU;Nie znaleziono użytkownika");
                        return;
                    }
                }

                switch (info[1])
                {
                    case "Get":
                        for(int i = 0;i<season.users.Count;i++)
                        {
                            if(season.users[i].user.id == target.id)
                            {
                                Season.SeasonUser.AccessLevels level = Season.SeasonUser.AccessLevels.user;
                                if (user.id == target.id) level = Season.SeasonUser.AccessLevels.owner;

                                Send(con, data.Key, "OK;" + season.users[i].Serialize(level));
                                return;
                            }
                        }
                        Send(con, data.Key, "NF;Użytkownik nie jest zarejestrowany w tym sezonie");

                        break;

                    default:
                        Send(con, data.Key, "UR;Nieznany tag sezonu");
                        break;
                }
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[NetworkData.SeasonUserRequest]");
                Send(con, data.Key, "IE;Błąd przetwarzania wniosku");
            }
        }

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
                            string str = "";
                            if (user.admin) 
                            {
                                str = season.Serialize(true);
                            }
                            else
                            {
                                str = season.Serialize(false, user);
                            }

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

                    case "ServerVersion":
                        Send(connection, data.Key, WindWingAppServer.protocolVersion.ToString());
                        break;

                    case "AppLatestVersion":
                        Send(connection, data.Key, WindWingAppServer.appLatestVersion.ToString());
                        break;

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
                if(info[3].StartsWith("RESET:"))
                {
                    if(user == null)
                    {
                        Debug.Log("[NetworkData.RegisterUserRequest] (" + con.IPRemoteEndPoint.Address + ") User not found");
                        Send(con, data.Key, "BU;Nie znaleziono użytkownika");
                        return;
                    }
                    user = server.GetUserByMail(info[2]);
                    if (user == null)
                    {
                        Send(con, data.Key, "BD;Błędne dane wymagane do resetu hasła");
                        Debug.Log("[NetworkData.RegisterUserRequest] User with that email account doesn't exists so cannot reset password");
                        return;
                    }
                    string token = info[3].Split(':')[1];
                    if(!server.ResetPassword(user, token, info[1]))
                    {
                        Debug.Log("[NetworkData.RegisterUserRequest] (" + con.IPRemoteEndPoint.Address + ") Bad password reset token");
                        Send(con, data.Key, "BT;Błędny token resetowania hasła");
                        return;
                    }
                    Debug.Log("[NetworkData.RegisterUserRequest] (" + con.IPRemoteEndPoint.Address + ") Password for user " + user.login + " resetted");
                    user.token = WindWingAppServer.GenerateToken();
                    Send(con, data.Key, "OK;" + user.token);
                    return;
                }

                User emptyPasswordUser = null;
                if (user != null)
                {
                    if (user.password.Length == 0)
                    {
                        emptyPasswordUser = user;
                    }
                    else
                    {
                        Send(con, data.Key, "UE;Uzytkownik o takiej nazwie juz istnieje");
                        Debug.Log("User already exists");
                        return;
                    }
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

                if(emptyPasswordUser != null)
                {
                    emptyPasswordUser.password = info[1];
                    emptyPasswordUser.token = WindWingAppServer.GenerateToken();
                    emptyPasswordUser.steam = info[3];
                    emptyPasswordUser.email = info[2];

                    server.RewriteUser(emptyPasswordUser);
                    Debug.Log("Completed user " + emptyPasswordUser.login + " registration (" + con.IPRemoteEndPoint.Address.ToString() + ")");
                    Send(con, data.Key, "OK;" + emptyPasswordUser.token.Replace(";", "\\:"));
                    return;
                }
                user = server.CreateUser(info[0], info[1], info[2], info[3], con.IPRemoteEndPoint.Address.ToString());
                server.RegisterUser(user);

                Debug.Log("Registered user " + user.login);

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
                if (info.Length < 8)
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


                var sUser = server.AddSeasonUser(season, user, TimeSpan.Parse(info[1]), TimeSpan.Parse(info[2]), info[3], info[4], Team.GetTeam(int.Parse(info[5])), Team.GetTeam(int.Parse(info[6])), Team.GetTeam(int.Parse(info[7])));

                if(sUser == null)
                {
                    Send(con, data.Key, "IE;Błąd dodawania użytkownika, zgłoś się do administratora o pomoc jeśli problem się powtórzy");
                    return;
                }

                Debug.Log("Registered to season user: " + user.login + " (" + user.id.ToString() + ")");
                Send(con, data.Key, "OK");
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[NetworkData.RegisterToSeason]");
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
                    Debug.Log("User is not admin");
                    return;
                }
                Debug.Log("Got admin request from user " + user.login);

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
                                    if (!season.Deserialize(info[3]))
                                    {
                                        Send(con, data.Key, "PE;Wystąpił błąd podczas przetwarzania żądania");

                                        return;
                                    }
                                    if(!server.UpdateSeasonSql(season))
                                    {
                                        Send(con, data.Key, "DE;Nie udało się zapisać danych sezonu do bazy danych");
                                        return;
                                    }
                                    Send(con, data.Key, "OK");
                                    season.Log();
                                    break;
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

                    case "GetUsers":
                        string dt = User.users.Count.ToString() + "{";
                        for(int i = 0;i<User.users.Count;i++)
                        {
                            dt += User.users[i].id.ToString();
                            if (i != User.users.Count - 1)
                            {
                                dt += ',';
                            }
                        }
                        dt += '}';
                        Send(con, data.Key, "OK;" + dt);
                        Debug.Log("User " + user.id + " (" + con.IPRemoteEndPoint.Address.ToString() + ") got users list");
                        return;

                    case "SeasonUser":
                        {
                            if (info.Length == 1)
                            {
                                Send(con, data.Key, "ER;Empty Request");
                                return;
                            }

                            if (info.Length < 4)
                            {
                                Send(con, data.Key, "BP;Brak wystarczajacej ilosci parametrow");
                                return;
                            }

                            var season = server.GetSeason(int.Parse(info[1]));
                            if (season == null)
                            {
                                Send(con, data.Key, "BS;Nie znaleziono sezonu");
                                return;
                            }
                            switch (info[2])
                            {
                                default:
                                    Send(con, data.Key, "BT;Błędny tag");
                                    return;
                            }
                            break;
                        }

                    case "RegisterSeason":
                        {
                            if (info.Length < 10)
                            {
                                Send(con, data.Key, "ND;Zbyt malo informacji");
                                return;
                            }

                            int seasonNmbr = int.Parse(info[1]);

                            Season season = server.GetSeason(seasonNmbr);
                            if (season == null)
                            {
                                Send(con, data.Key, "BS;Bledny numer sezonu");
                                return;
                            }

                            User nUser = User.GetUser(int.Parse(info[9]));

                            var sUser = server.AddSeasonUser(season, nUser, TimeSpan.Parse(info[2]), TimeSpan.Parse(info[3]), info[4], info[5], Team.GetTeam(int.Parse(info[6])), Team.GetTeam(int.Parse(info[7])), Team.GetTeam(int.Parse(info[8])));

                            if (sUser == null)
                            {
                                Send(con, data.Key, "IE;Błąd dodawania użytkownika, zgłoś się do administratora o pomoc jeśli problem się powtórzy");
                                return;
                            }

                            Debug.Log("Registered to season " + season.id.ToString() + " user: " + nUser.login + " by admin " + user.login + " (" + user.id.ToString() + ")");
                            Send(con, data.Key, "OK");
                            break;
                        }

                    case "CreateUser":
                        {
                            if(info.Length < 2) // CreateUser;login
                            {
                                Send(con, data.Key, "ND;Zbyt malo informacji");
                                return;
                            }
                            User u = server.CreateUser(info[1], "", "", "", "", false, "");
                            server.RegisterUser(u);
                            Send(con, data.Key, "OK");
                            break;
                        }

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

                #if DEBUG
                {
                    if (info[0] == "Minik")
                    {
                        User u = server.GetUser("Minik");

                        u.connection = con;
                        loggedIn.Add(u);
                        Send(con, data.Key, "OK;" + u.login + ";" + u.admin);

                        Debug.Log("User Minik logged in without using token [ DEBUG ]");
                        if (u.admin)
                        {
                            Debug.Log("Adding Admin request handler");
                            con.RegisterRawDataHandler("Admin", AdminRequest);
                        }

                        return;
                    }
                }
                #endif


                info[1] = info[1].Replace("\\:", ";");

                User user = server.GetUserByToken(info[0], info[1], false);
                if (user == null)
                {
                    User u = server.GetUser(info[0]);
                    Debug.Log("(" + con.IPRemoteEndPoint.Address + ") Bad credentials with login: \"" + info[0] + "\"");
                    Send(con, data.Key, "BC;Bledny login lub token, zaloguj sie ponownie");
                    return;
                }

                if(user.password.Length == 0 || user.token.Length == 0)
                {
                    Send(con, data.Key, "NL;Możliwość logowania dla tego użytkownika została zablokowana");
                    Debug.Log("(" + con.IPRemoteEndPoint.Address.ToString() + ") tried to log in uncompleted registration user");
                    return;
                }

                user.connection = con;
                loggedIn.Add(user);
                Send(con, data.Key, "OK;" + user.login + ";" + user.admin);

                if(user.admin)
                {
                    Debug.Log("Adding Admin request handler");
                    con.RegisterRawDataHandler("Admin", AdminRequest);
                }

                Debug.Log("User " + user.login + " [" + con.IPRemoteEndPoint.ToString() + "] logged in using token");
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

                #if DEBUG
                {
                    if (info[0] == "Minik")
                    {
                        User u = server.GetUser("Minik");

                        u.connection = con;
                        loggedIn.Add(u);
                        Send(con, data.Key, "OK;" + u.login + ";" + u.token + ";" + u.admin);

                        Debug.Log("User Minik logged in without using token [ DEBUG ]");
                        if (u.admin)
                        {
                            Debug.Log("Adding Admin request handler");
                            con.RegisterRawDataHandler("Admin", AdminRequest);
                        }

                        return;
                    }
                }
                #endif

                info[1] = info[1].Replace("\\:", ";");

                User user = server.GetUser(info[0], info[1], false);
                if (user == null)
                {
                    Debug.Log("(" + con.IPRemoteEndPoint.Address + ") Bad credentials");
                    Send(con, data.Key, "BC;Bledny login lub haslo");
                    return;
                }

                if (user.password.Length == 0 || user.token.Length == 0)
                {
                    Send(con, data.Key, "NL;Możliwość logowania dla tego użytkownika została zablokowana");
                    Debug.Log("(" + con.IPRemoteEndPoint.Address.ToString() + ") tried to log in uncompleted registration user");
                    return;
                }

                user.connection = con;
                loggedIn.Add(user);
                Send(con, data.Key, "OK;" + user.login + ";" + user.token.Replace(";", "\\:") + ";" + user.admin);

                if (user.admin)
                {
                    Debug.Log("Adding Admin request handler");
                    con.RegisterRawDataHandler("Admin", AdminRequest);
                }

                Debug.Log("User " + user.login + " [" + con.IPRemoteEndPoint.ToString() + "] logged in using password");
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
