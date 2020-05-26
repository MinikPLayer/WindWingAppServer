using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using System.Threading;

using WindWingAppServer.Models;

namespace WindWingAppServer
{
    public class WindWingAppServer
    {
        MSQL sql;
        NetworkData networkData;

        const int version = 1;


        //public List<User> users = new List<User>();

        public List<Season> seasons = new List<Season>();

        int usersCount = 0;

        public Season.SeasonUser AddSeasonUser(Season season, User user, TimeSpan timeDry, TimeSpan timeWet, string dryLink, string wetLink)
        {
            var sUser = new Season.SeasonUser(user, timeDry, timeWet, dryLink, wetLink);

            for(int i = 0;i<season.users.Count;i++)
            {
                if(season.users[i].user.id == user.id)
                {
                    return null;
                }
            }

            if(!sql.AddEntry(season.prefix + "users", sUser.ToSqlValues()))
            {
                return null;
            }

            season.users.Add(sUser);

            sql.ModifyEntry("seasons", new MSQL.Value("userscount", season.users.Count), "id=" + season.id.ToString());

            return sUser;
        }

        public RegistrationData[] GetRegistrationData()
        {
            List<RegistrationData> datas = new List<RegistrationData>();
            for(int i = 0;i<seasons.Count;i++)
            {
                if(seasons[i].registrationData.opened)
                {
                    datas.Add(seasons[i].registrationData);
                }
            }

            return datas.ToArray();
        }

        bool workingWithUsers = false;
        public void RegisterUser(User user)
        {
            while(workingWithUsers)
            {
                Thread.Sleep(10);
            }
            workingWithUsers = true;
            sql.AddEntry("users", user.ToSqlValues());
            sql.ModifyEntry("dbinfo", new MSQL.Value("userscount", usersCount));
            User.users.Add(user);
            workingWithUsers = false;
        }

        void RewriteUser(User user)
        {
            while (workingWithUsers)
            {
                Thread.Sleep(10);
            }
            workingWithUsers = true;

            sql.RemoveEntry("users", new MSQL.Value("id", user.id));
            sql.AddEntry("users", user.ToSqlValues());

            workingWithUsers = false;
        }

        void AddUser(User user)
        {
            User.users.Add(user);
        }

        public void AddSeason(Season s)
        {
            WriteSeasonTable(s);
            seasons.Add(s);
        }

        public bool RemoveSeason(int id)
        {
            int index = -1;
            Season s = null;
            for(int i = 0;i<seasons.Count;i++)
            {
                if(seasons[i].id == id)
                {
                    index = i;
                    s = seasons[i];
                }
            }
            if(s == null)
            {
                Debug.LogError("Season with id " + id.ToString() + " not found");
                return false;
            }

            sql.RemoveEntry("seasons", new MSQL.Value("id", s.id));
            if(s.prefix.Length == 0)
            {
                s.prefix = "s" + s.id.ToString() + "_";
            }
            if(sql.TableExists(s.prefix + "users"))
            {
                sql.DropTable(s.prefix + "users");
            }
            if(sql.TableExists(s.prefix + "races"))
            {
                sql.DropTable(s.prefix + "races");
            }
            if(sql.TableExists(s.prefix + "results"))
            {
                sql.DropTable(s.prefix + "results");
            }

            seasons.RemoveAt(index);

            int count = sql.ReadEntry<int>("dbinfo", new MSQL.Column("seasonsCount", MSQL.ColumnType.INT))[0];
            sql.ModifyEntry("dbinfo", new MSQL.Value("seasonsCount", count - 1));

            return !sql.error;
        }

        User CreateUser(object[] data)
        {
            User u = new User();

            u.LoadFromSql(data);

            return u;
        }

        string GenerateToken()
        {
            string val = "";
            Random rand = new Random();
            for(int i = 0;i<32;i++)
            {
                char c = (char)rand.Next(33, 126);
                if (c == ';') c = ':';
                val += c;
            }

            return val;
        }

        public User CreateUser(string login, string password, string email, string steam, string ip, bool admin = false, string token = null)
        {
            if(token == null)
            {
                token = GenerateToken();
            }

            return new User(usersCount++, login, password, token, email, steam, ip, admin);
        }

        bool WriteSeasonTable(Season season, bool overwrite = false)//int season, int usersCount, int racesCount, bool finished, bool overwrite = false)
        {
            Debug.Log("Seasons found: ");
            object[] ids = sql.ReadEntry("seasons", new MSQL.Column("id", MSQL.ColumnType.INT)); //new List<string>() { "id" }, new List<MSQL.ColumnType>() { MSQL.ColumnType.INT });
            for(int i = 0;i<ids.Length;i++)
            {

                if((int)ids[i] == season.id)
                {
                    if (overwrite)
                    {
                        Debug.LogWarning("[WWAS.WriteSeasonTable] Season with id " + season + " already found");
                        sql.RemoveEntry("seasons", new MSQL.Value("id", season.id));
                    }
                    else
                    {
                        Debug.LogError("[WWAS.WriteSeasonTable] Season with id " + season + " already found");
                        return false;
                    }
                }
            }

            string prefix = "s" + season.id.ToString() + "_";
             

            sql.AddEntry("seasons", new List<MSQL.Value>() {
                new MSQL.Value("id", season.id.ToString()),
                new MSQL.Value("userscount", season.users.Count),
                new MSQL.Value("prefix", prefix),
                new MSQL.Value("racescount", season.racesCount),
                new MSQL.Value("finishedraces", season.finishedRaces),
                new MSQL.Value("registrationend", season.registrationData.endDate),
                new MSQL.Value("timetrackid", season.registrationTrack.id)
            });

            int count = sql.ReadEntry<int>("dbinfo", new MSQL.Column("seasonsCount", MSQL.ColumnType.INT))[0];
            sql.ModifyEntry("dbinfo", new MSQL.Value("seasonsCount", count + 1));

            sql.CreateTable(prefix + "races", new List<MSQL.Column>() {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("trackid",  MSQL.ColumnType.INT),
                new MSQL.Column("date",  MSQL.ColumnType.DATETIME),
                new MSQL.Column("resultstable", MSQL.ColumnType.STRING)
            });

            sql.CreateTable(prefix + "users", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("lapdry", MSQL.ColumnType.TIME),
                new MSQL.Column("lapwet", MSQL.ColumnType.TIME),
                new MSQL.Column("lapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("lapwetlink", MSQL.ColumnType.STRING),
                new MSQL.Column("priority", MSQL.ColumnType.TIME)
            });


            try
            {
                for (int i = 0; i < season.races.Count; i++)
                {
                    sql.AddEntry(prefix + "races", new List<MSQL.Value>
                    {
                        new MSQL.Value("id", i),
                        new MSQL.Value("trackid", season.races[i].track.id),
                        new MSQL.Value("date", season.races[i].date),
                        new MSQL.Value("resultstable", (season.races[i].results == null ) ? "" : prefix + "results_" + i.ToString())
                    });
                }
            }
            catch(Exception e)
            {
                Debug.Exception(e, "Error adding season races: ");
            }
                
            return true;
        }


        void FillTracksTable()
        {
            sql.AddEntries("tracks", new List<string>() { "id", "name", "country", "city", "tracklength", "trackrecord" }, Track.TracksToSQL());
        }

        private void FillTeamsTable()
        {
            sql.AddEntries("teams", new List<string>() { "id", "name", "shortname", "iconpath" }, Team.TeamsToSQL());
        }

        void WriteSeason1Data()
        {
            //WriteSeasonTable(1, 25, 12, true);

            Season season = new Season(1, 12, 12, "S1_", Track.GetTrack(0), new List<Race>
            {
                new Race(0, 0, new DateTime(2020, 05, 04, 20,00,00)),
                new Race(1, 1, new DateTime(2020, 05, 05, 20,00,00)),
                new Race(2, 4, new DateTime(2020, 05, 06, 20,00,00)),
                new Race(3, 6, new DateTime(2020, 05, 07, 20,00,00)),
                new Race(4, 9, new DateTime(2020, 05, 08, 20,00,00)),
                new Race(5, 12, new DateTime(2020, 05, 11, 20,00,00)),
                new Race(6, 16, new DateTime(2020, 05, 12, 20,00,00)),
                new Race(7, 17, new DateTime(2020, 05, 13, 20,00,00)),
                new Race(8, 3, new DateTime(2020, 05, 14, 20,00,00)),
                new Race(9, 18, new DateTime(2020, 05, 18, 20,00,00)),
                new Race(10, 10, new DateTime(2020, 05, 19, 20,00,00)),
                new Race(11, 11, new DateTime(2020, 05, 20, 20,00,00))
            }, new RegistrationData(DateTime.MinValue));

            season.registrationTrack = Track.GetTrack(0);

            WriteSeasonTable(season);

            seasons.Add(season);
        }

        void CreateDBStructure()
        {
            sql.CreateTable("dbinfo", new List<MSQL.Column>() { 
                new MSQL.Column("version", MSQL.ColumnType.INT), 
                new MSQL.Column("seasonscount", MSQL.ColumnType.INT),
                new MSQL.Column("userscount", MSQL.ColumnType.INT)
            });
            //sql.AddEntry("dbinfo", new List<string>() { "version", "seasonsCount" }, new List<string>() { version.ToString(), "0" });
            sql.AddEntry("dbinfo", new List<MSQL.Value>()
            {
                new MSQL.Value("version", version.ToString()),
                new MSQL.Value("seasonscount", "0"),
                new MSQL.Value("userscount", "0")
            });

            sql.CreateTable("tracks", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("name", MSQL.ColumnType.STRING),
                new MSQL.Column("country", MSQL.ColumnType.STRING),
                new MSQL.Column("city", MSQL.ColumnType.STRING),
                new MSQL.Column("tracklength", MSQL.ColumnType.INT),
                new MSQL.Column("trackrecord", MSQL.ColumnType.TIME)            
            });

            sql.CreateTable("teams", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("name", MSQL.ColumnType.STRING),
                new MSQL.Column("shortname", MSQL.ColumnType.STRING),
                new MSQL.Column("iconpath", MSQL.ColumnType.STRING)
            });

            sql.CreateTable("seasons", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("userscount", MSQL.ColumnType.INT),
                new MSQL.Column("racescount", MSQL.ColumnType.INT),
                new MSQL.Column("finishedraces", MSQL.ColumnType.INT),
                new MSQL.Column("prefix", MSQL.ColumnType.STRING),
                new MSQL.Column("registrationend", MSQL.ColumnType.DATETIME),
                new MSQL.Column("timetrackid", MSQL.ColumnType.INT)
            });

            sql.CreateTable("users", new List<MSQL.Column>() {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("login", MSQL.ColumnType.STRING),
                new MSQL.Column("password", MSQL.ColumnType.STRING),
                new MSQL.Column("token", MSQL.ColumnType.STRING),
                new MSQL.Column("email", MSQL.ColumnType.STRING),
                new MSQL.Column("steam", MSQL.ColumnType.STRING),
                new MSQL.Column("ip", MSQL.ColumnType.STRING),
                new MSQL.Column("admin", MSQL.ColumnType.BOOLEAN)
            });

            FillTracksTable();
            FillTeamsTable();

            WriteSeason1Data();

            //RegisterUser(CreateUser("Minik", "", "", "", "", true, ""));
        }



        void LoadUsersFromDB()
        {
            List<object[]> objects = sql.ReadEntries("users", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("login", MSQL.ColumnType.STRING),
                new MSQL.Column("password", MSQL.ColumnType.STRING),
                new MSQL.Column("token", MSQL.ColumnType.STRING),
                new MSQL.Column("email", MSQL.ColumnType.STRING),
                new MSQL.Column("steam", MSQL.ColumnType.STRING),
                new MSQL.Column("admin", MSQL.ColumnType.BOOLEAN)
            });

            if (objects.Count == 0) return;

            /*List<object[]> uObjects = new List<object[]>(objects[0].Length);
            for(int i = 0;i<objects[0].Length; i++)
            {
                object[] data = new object[objects.Count];
                for(int j = 0;j<objects.Count;j++)
                {
                    data[j] = objects[j][i];
                }
                uObjects.Add(data);
            }*/
            var uObjects = MUtil.FlipSQLData(objects);
            for(int i = 0;i<uObjects.Count;i++)
            {
                User u = CreateUser(uObjects[i]);
                AddUser(u);
            }
        }

        bool LoadSeasonUsersFromDB(Season season)
        {
            if(season.users.Capacity == 0)
            {
                return true;
            }

            List<object[]> objects = sql.ReadEntries(season.prefix + "users", new List<MSQL.Column>
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("lapdry", MSQL.ColumnType.TIME),
                new MSQL.Column("lapwet", MSQL.ColumnType.TIME),
                new MSQL.Column("lapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("lapwetlink", MSQL.ColumnType.STRING),
                new MSQL.Column("priority", MSQL.ColumnType.INT)
            });

            List<object[]> uObjects = MUtil.FlipSQLData(objects);
            for(int i = 0;i<uObjects.Count;i++)
            {
                Season.SeasonUser newUser = new Season.SeasonUser();
                if(!newUser.LoadFromSql(uObjects[i]))
                {
                    Debug.LogError("[WindWingAppServer.LoadSeasonUsersFromDB] Error loading users");
                    return false;
                }

                season.users.Add(newUser);
            }

            return true;
        }

        bool LoadSeasonRacesFromDB(Season season)
        {
            if(season.racesCount == 0)
            {
                return true;
            }

            List<object[]> objects = sql.ReadEntries(season.prefix + "races", new List<MSQL.Column>
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("trackid", MSQL.ColumnType.INT),
                new MSQL.Column("date", MSQL.ColumnType.DATETIME),
                new MSQL.Column("resultstable", MSQL.ColumnType.STRING)
            });

            List<object[]> uObjects = MUtil.FlipSQLData(objects);
            for(int i = 0;i<uObjects.Count;i++)
            {
                Race newRace = new Race();
                if (!newRace.LoadFromSql(uObjects[i]))
                {
                    Debug.LogError("[WindWingAppServer.LoadSeasonRacesFromDB] Cannot load race " + i.ToString());
                    return false;
                }

                season.races.Add(newRace);
            }

            return true;
        }

        bool LoadSeasonsFromDB()
        {
            try
            {
                List<object[]> objects = sql.ReadEntries("seasons", new List<MSQL.Column>
                {
                    new MSQL.Column("id", MSQL.ColumnType.INT),
                    new MSQL.Column("userscount", MSQL.ColumnType.INT),
                    new MSQL.Column("racescount", MSQL.ColumnType.INT),
                    new MSQL.Column("finishedraces", MSQL.ColumnType.INT),
                    new MSQL.Column("prefix", MSQL.ColumnType.STRING),
                    new MSQL.Column("registrationend", MSQL.ColumnType.DATETIME),
                    new MSQL.Column("timetrackid", MSQL.ColumnType.INT)
                });

                var uObjects = MUtil.FlipSQLData(objects);
                for (int i = 0; i < uObjects.Count; i++)
                {
                    Season s = new Season(uObjects[i]);
                    if (!s.good)
                    {
                        Debug.LogError("[WindWingAppServer.LoadSeasonsFromDB] Error loading season " + i + ", id: " + ((int)uObjects[i][0]).ToString());
                        return false;
                    }

                    if(!LoadSeasonUsersFromDB(s))
                    {
                        Debug.LogError("[WindWingAppServer.LoadSeasonsFromDB] Cannot load season users");
                        return false;
                    }
                    if(!LoadSeasonRacesFromDB(s))
                    {
                        Debug.LogError("[WindWingAppServer.LoadSeasonsFromDB] Cannot load season races");
                        return false;
                    }
                    //s.Log();

                    seasons.Add(s);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[WindWingAppServer.LoadSeasonFromDB]");
                return false;
            }
        }

        void GetDBData()
        {
            LoadUsersFromDB();
            LoadSeasonsFromDB();
        }

        public string GenerateLeaderboardsString(int season)
        {
            // Placeholder
            if (season == 1)
            {
                return "25{(Quorthon,162,RDB);(Minik,127,MCL);(kypE,134,RDB);(BARTEQ,75,HAS);(Skomek,80,TRS);(Rogar2630,61,FRI);(Patryk913,84,TRS);(Giro,68,ARO);(Yomonoe,44,MCL);(Copy JR,39,RNL);(R4zor,37,MER);(slepypirat,34,RPT);(koczejk,26,HAS);(Shiffer,26,RPT);(cichy7220,23,MER);(Allu,21,OTH);(Myslav,14,OTH);(Hokejode,11,ARO);(Paw3lo,10,OTH);(Lewandor,16,OTH);(xVenox,1,OTH);(Kamilos61,0,RNL);(Grok12,-1,FRI);([SOL]NikoMon,-2,OTH);(Bany,-2,OTH)}";
            }
            else
            {
                return "NS";
            }
        }

        public Season GetSeason(int number)
        {
            for(int i = 0;i<seasons.Count;i++)
            {
                if(seasons[i].id == number)
                {
                    return seasons[i];
                }
            }

            return null;
        }


        public User GetUser(string login, bool caseSensitive = false)
        {
            if(caseSensitive)
            {
                return GetUserCS(login);
            }
            else
            {
                return GetUserNCS(login);
            }
        }

        public User GetUserNCS(string login)
        {
            login = login.ToLower();
            for (int i = 0; i < User.users.Count; i++)
            {
                if (User.users[i].login.ToLower() == login)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        public User GetUserCS(string login)
        {
            for (int i = 0; i < User.users.Count; i++)
            {
                if (User.users[i].login == login)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        public User GetUserByMail(string email)
        {
            for (int i = 0; i < User.users.Count; i++)
            {
                if (User.users[i].email == email)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        public User GetUserBySteam(string steam)
        {
            for (int i = 0; i < User.users.Count; i++)
            {
                if (User.users[i].steam == steam)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        public User GetUserByToken(string login, string token, bool caseSensitive = false)
        {
            if(caseSensitive)
            {
                return GetUserByTokenCS(login, token);
            }
            else
            {
                return GetUserByTokenNCS(login, token);
            }
        }

        public User GetUserByTokenCS(string login, string token)
        {
            for (int i = 0; i < User.users.Count; i++)
            {
                if (User.users[i].login == login && User.users[i].token == token)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        public User GetUserByTokenNCS(string login, string token)
        {
            login = login.ToLower();

            for (int i = 0; i < User.users.Count; i++)
            {
                if (User.users[i].login.ToLower() == login && User.users[i].token == token)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        public User GetUser(string login, string password, bool caseSensitive = false)
        {
            if(caseSensitive)
            {
                return GetUserCS(login, password);
            }
            else
            {
                return GetUserNCS(login, password);
            }
        }

        public User GetUserCS(string login, string password)
        {
            for(int i = 0;i< User.users.Count;i++)
            {
                if(User.users[i].login == login && User.users[i].password == password)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        public User GetUserNCS(string login, string password)
        {
            login = login.ToLower();
            for (int i = 0; i < User.users.Count; i++)
            {
                if (User.users[i].login.ToLower() == login && User.users[i].password == password)
                {
                    return User.users[i];
                }
            }
            return null;
        }

        const bool clear = false;
        public WindWingAppServer(bool clearAllData = false)
        {
            Debug.Log("Loading WindWingAppServer version 0.5...");

            sql = new MSQL("localhost", "WindWingApp", "windWingStrongPass", "WindWingApp");

            if(clear || clearAllData)
            {
                sql.DropAllTables();
                Debug.Log("Clearing complete");
            }


            bool exists = sql.TableExists("dbinfo");
            if (exists)
            {
                Debug.Log("Loading data from DB...");
                GetDBData();
            }
            else
            {
                Debug.Log("Creating data in DB...");
                CreateDBStructure();
            }

            Debug.Log("Done");

            Debug.Log("Starting a network socket");
            networkData = new NetworkData(this, 8148);
            Debug.Log("Done");

            while(true)
            {
                if(Console.KeyAvailable)
                {
                    string cmd = Console.ReadLine();
                    ParseCommand(cmd);
                }

                Thread.Sleep(100);
            }
        }

        void ParseCommand(string command)
        {
            try
            {
                string[] parts = command.Split(' ');

                bool ok = true;
                switch (parts[0])
                {
                    case "setadmin":
                        User u = GetUser(parts[1]);
                        
                        if(parts.Length > 2 && parts[2] == "0")
                        {
                            u.admin = false;
                            sql.ExecuteCommand("UPDATE users SET admin=false WHERE id=" + u.id.ToString());
                        }
                        else
                        {
                            u.admin = true;
                            sql.ExecuteCommand("UPDATE users SET admin=true WHERE id=" + u.id.ToString());
                        }

                        Debug.Log("User " + u.login + " is now an admin");
                        break;

                    default:
                        Debug.LogError("[Command] Unknown command");
                        ok = false;
                        break;
                }

                if (ok)
                {
                    Debug.Log("OK");
                }
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[Command] Parsing exception: ");
            }
        }
    }
}
