using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using System.Threading;

using WindWingAppServer.Models;

namespace WindWingAppServer
{
    public class WindWingAppServer
    {
        MSQL sql;
        NetworkData networkData;

        public const string appVersion = "0.7.0a2";
        public const int protocolVersion = 4;
        public static int appLatestVersion = 52;

        //public List<User> users = new List<User>();

        public List<Season> seasons = new List<Season>();

        bool workingWithPasses = false;
        public List<ResetPass> resetPasses = new List<ResetPass>();

        public Season.SeasonUser AddSeasonUser(Season season, User user, TimeSpan timeDry, TimeSpan timeWet, string dryLink, string wetLink, Team team1, Team team2, Team team3)
        {
            try
            {
                var sUser = new Season.SeasonUser(user, timeDry, timeWet, dryLink, wetLink, team1, team2, team3);

                for (int i = 0; i < season.users.Count; i++)
                {
                    if (season.users[i].user.id == user.id)
                    {
                        if (!sql.RemoveEntry(season.prefix + "users", new MSQL.Value("id", sUser.user.id)))
                        {
                            Debug.LogError("[WindWingAppServer.AddSeasonUser] User exists - Error deleting season user data");
                            return null;
                        }

                        if (!sql.AddEntry(season.prefix + "users", sUser.ToSqlValues()))
                        {
                            Debug.LogError("[WindWingAppServer.AddSeasonUser] User exists - Error adding season user data");
                            return null;
                        }

                        season.users[i] = sUser;

                        return sUser;
                    }
                }

                if (!sql.AddEntry(season.prefix + "users", sUser.ToSqlValues()))
                {
                    Debug.LogError("[WindWingAppServer.AddSeasonUser] Error adding season user data");
                    return null;
                }

                season.users.Add(sUser);


                if (season.assigned)
                {
                    //Send(con, data.Key, "RC;Rejestracja do tego sezonu została już zamknięta");
                    season.AssignDriverAfterRegistartion(sUser);
                    UpdateSeasonSql(season);
                }

                sql.ModifyEntry("seasons", new MSQL.Value("userscount", season.users.Count), "id=" + season.id.ToString());

                return sUser;


            }
            catch(Exception e)
            {
                Debug.Exception(e, "[WindWingAppServer.AddSeasonUser]");
                return null;
            }
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
        public bool RegisterUser(User user)
        {
            try
            {
                while (workingWithUsers)
                {
                    Thread.Sleep(10);
                }
                workingWithUsers = true;
                //sql.ModifyEntry("dbinfo", new MSQL.Value("userscount", usersCount));
                int count = sql.ReadEntry<int>("dbinfo", new MSQL.Column("userscount", MSQL.ColumnType.INT))[0];
                Debug.Log("User id: " + user.id);
                sql.AddEntry("users", user.ToSqlValues());
                sql.ModifyEntry("dbinfo", new MSQL.Value("userscount", count + 1));
                User.users.Add(user);
                workingWithUsers = false;
                return true;
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[WindWingAppServer.RegisterUser]");
                return false;
            }
        }

        public void RewriteUser(User user)
        {
            if(user == null)
            {
                Debug.LogError("[WindWingAppServer.RewriteUser] User is null");
                return;
            }

            while (workingWithUsers)
            {
                Thread.Sleep(10);
            }
            workingWithUsers = true;

            //sql.RemoveEntry("users", new MSQL.Value("id", user.id));
            //sql.AddEntry("users", user.ToSqlValues());
            sql.ModifyEntries("users", user.ToSqlValues(), "id=" + user.id);

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

        public bool UpdateSeasonSql(Season s)
        {
            return WriteSeasonTable(s, true);
        }

        User CreateUser(object[] data)
        {
            User u = new User();

            u.LoadFromSql(data);

            return u;
        }

        public static string GenerateToken()
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

        List<int> reservedIDs = new List<int>();
        bool generatingIDs = false;
        int GenerateUserID()
        {
            while(generatingIDs)
            {
                Thread.Sleep(10);
            }
            generatingIDs = true;

            int id = -1;
            while (true)
            {
                id = new Random().Next();
                User u = User.GetUser(id);
                if(u != null)
                {
                    continue;
                }

                for(int i = 0;i<reservedIDs.Count;i++)
                {
                    if(reservedIDs[i] == id)
                    {
                        continue;
                    }
                }
                break;
            }

            reservedIDs.Add(id);
            generatingIDs = false;

            return id;
        }

        public User CreateUser(string login, string password, string email, string steam, string ip, bool admin = false, string token = null)
        {
            if(token == null)
            {
                token = GenerateToken();
            }

            return new User(GenerateUserID(), login, password, token, email, steam, ip, admin);
        }

        bool WriteSeasonTable(Season season, bool overwrite = false)//int season, int usersCount, int racesCount, bool finished, bool overwrite = false)
        {
            bool overwritten = false;
            string prefix = "s" + season.id.ToString() + "_";

            Debug.Log("Seasons found: ");
            object[] ids = sql.ReadEntry("seasons", new MSQL.Column("id", MSQL.ColumnType.INT)); //new List<string>() { "id" }, new List<MSQL.ColumnType>() { MSQL.ColumnType.INT });
            for(int i = 0;i<ids.Length;i++)
            {

                if((int)ids[i] == season.id)
                {
                    if (overwrite)
                    {
                        Debug.LogWarning("[WWAS.WriteSeasonTable] Season with id " + season + " already found");
                        //sql.RemoveEntry("seasons", new MSQL.Value("id", season.id));
                        sql.ModifyEntries("seasons", new List<MSQL.Value>
                        {
                            new MSQL.Value("id", season.id.ToString()),
                            new MSQL.Value("userscount", season.users.Count),
                            new MSQL.Value("prefix", prefix),
                            new MSQL.Value("racescount", season.racesCount),
                            new MSQL.Value("finishedraces", season.finishedRaces),
                            new MSQL.Value("registrationend", season.registrationData.endDate),
                            new MSQL.Value("timetrackid", season.registrationTrack.id),
                            new MSQL.Value("assigned", season.assigned)
                        }, "id=" + season.id.ToString());
                        overwritten = true;

                        if(sql.TableExists(prefix + "users"))
                        {
                            if(sql.TableExists(prefix + "users_bak"))
                            {
                                sql.DropTable(prefix + "users_bak");
                            }
                            sql.CopyTable(prefix + "users", prefix + "users_bak");
                            sql.DropTable(prefix + "users");
                        }
                        if(sql.TableExists(prefix + "races"))
                        {
                            if (sql.TableExists(prefix + "races_bak"))
                            {
                                sql.DropTable(prefix + "races_bak");
                            }
                            sql.CopyTable(prefix + "races", prefix + "races_bak");
                            sql.DropTable(prefix + "races");
                        }
                    }
                    else
                    {
                        Debug.LogError("[WWAS.WriteSeasonTable] Season with id " + season + " already found");
                        return false;
                    }
                }
            }

            

            if (!overwritten)
            {
                sql.AddEntry("seasons", new List<MSQL.Value>() {
                    new MSQL.Value("id", season.id.ToString()),
                    new MSQL.Value("userscount", season.users.Count),
                    new MSQL.Value("prefix", prefix),
                    new MSQL.Value("racescount", season.racesCount),
                    new MSQL.Value("finishedraces", season.finishedRaces),
                    new MSQL.Value("registrationend", season.registrationData.endDate),
                    new MSQL.Value("timetrackid", season.registrationTrack.id),
                    new MSQL.Value("assigned", season.assigned)
                });

                int count = sql.ReadEntry<int>("dbinfo", new MSQL.Column("seasonsCount", MSQL.ColumnType.INT))[0];
                sql.ModifyEntry("dbinfo", new MSQL.Value("seasonsCount", count + 1));
            }

            sql.CreateTable(prefix + "races", new List<MSQL.Column>() {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("trackid",  MSQL.ColumnType.INT),
                new MSQL.Column("date",  MSQL.ColumnType.DATETIME),
                new MSQL.Column("resultstable", MSQL.ColumnType.STRING)
            });

            sql.CreateTable(prefix + "users", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("lapdry", MSQL.ColumnType.TIME, false),
                new MSQL.Column("lapwet", MSQL.ColumnType.TIME, false),
                new MSQL.Column("lapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("lapwetlink", MSQL.ColumnType.STRING),
                new MSQL.Column("priority", MSQL.ColumnType.TIME, false),
                new MSQL.Column("team", MSQL.ColumnType.INT),
                new MSQL.Column("pteam1", MSQL.ColumnType.INT),
                new MSQL.Column("pteam2", MSQL.ColumnType.INT),
                new MSQL.Column("pteam3", MSQL.ColumnType.INT)
            });


            try
            {
                for (int i = 0; i < season.users.Count; i++)
                {
                    sql.AddEntry(prefix + "users", season.users[i].ToSqlValues());
                }
            }
            catch (Exception e)
            {
                Debug.Exception(e, "Error adding season users: ");
            }

            try
            {
                for (int i = 0; i < season.races.Count; i++)
                {
                    string tableName = "";
                    if(season.races[i].results != null)
                    {
                        tableName = prefix + "results_" + season.races[i].id.ToString();

                        if(sql.TableExists(tableName))
                        {
                            if(sql.TableExists(tableName + "_bak"))
                            {
                                sql.DropTable(tableName + "_bak");
                            }
                            sql.CopyTable(tableName, tableName + "_bak");
                            sql.DropTable(tableName);
                        }

                        sql.CreateTable(tableName, new List<MSQL.Column>
                        {
                            new MSQL.Column("user", MSQL.ColumnType.INT),
                            new MSQL.Column("place", MSQL.ColumnType.INT),
                            new MSQL.Column("bestlap", MSQL.ColumnType.TIME, false),
                            new MSQL.Column("time", MSQL.ColumnType.TIME, false),
                            new MSQL.Column("dnf", MSQL.ColumnType.BOOLEAN),
                            new MSQL.Column("started", MSQL.ColumnType.BOOLEAN)
                        });

                        Debug.Log("Creating table for race: " + i.ToString());
                        for(int j = 0;j<season.races[i].results.Count;j++)
                        {
                            Debug.Log("\tAdding entry for user " + j.ToString());
                            sql.AddEntry(tableName, new List<MSQL.Value>
                            {
                                new MSQL.Value("user", season.races[i].results[j].user.id),
                                new MSQL.Value("place", season.races[i].results[j].place),
                                new MSQL.Value("bestlap", season.races[i].results[j].bestLap),
                                new MSQL.Value("time", season.races[i].results[j].time),
                                new MSQL.Value("dnf", season.races[i].results[j].dnf),
                                new MSQL.Value("started", season.races[i].results[j].started)
                            });
                        }
                    }

                    season.races[i].resultsTable = tableName;
                    sql.AddEntry(prefix + "races", new List<MSQL.Value>
                    {
                        new MSQL.Value("id", i),
                        new MSQL.Value("trackid", season.races[i].track.id),
                        new MSQL.Value("date", season.races[i].date),
                        new MSQL.Value("resultstable", tableName)
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
                new MSQL.Value("version", protocolVersion.ToString()),
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
                new MSQL.Column("trackrecord", MSQL.ColumnType.TIME, false)            
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
                new MSQL.Column("timetrackid", MSQL.ColumnType.INT),
                new MSQL.Column("assigned", MSQL.ColumnType.BOOLEAN)
            });

            sql.CreateTable("users", new List<MSQL.Column>() {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("login", MSQL.ColumnType.STRING),
                new MSQL.Column("password", MSQL.ColumnType.STRING),
                new MSQL.Column("token", MSQL.ColumnType.STRING),
                new MSQL.Column("email", MSQL.ColumnType.STRING),
                new MSQL.Column("steam", MSQL.ColumnType.STRING),
                new MSQL.Column("ip", MSQL.ColumnType.STRING),
                new MSQL.Column("admin", MSQL.ColumnType.BOOLEAN),
                new MSQL.Column("donate", MSQL.ColumnType.INT)
            });

            FillTracksTable();
            FillTeamsTable();

            WriteSeason1Data();

            if (MUtil.debug)
            {
                User u = CreateUser("Minik", "", "", "", "", true, "");
                RegisterUser(u);

                DebugAddUserAndSeasonUser(seasons[0], "medium", new TimeSpan(0, 0, 1, 12, 532), new TimeSpan(0, 0, 1, 25, 532), Team.teams[1], Team.teams[2], Team.teams[3], "https://steamcommunity.com/id/MinikPlayer2/");
                DebugAddUserAndSeasonUser(seasons[0], "very slow", new TimeSpan(0, 0, 1, 17, 151), new TimeSpan(0, 0, 1, 32, 151), Team.teams[1], Team.teams[2], Team.teams[3]);
                DebugAddUserAndSeasonUser(seasons[0], "very very slow", new TimeSpan(0, 0, 1, 20, 812), new TimeSpan(0, 0, 1, 35, 713), Team.teams[1], Team.teams[2], Team.teams[3]);
                DebugAddUserAndSeasonUser(seasons[0], "fast", new TimeSpan(0, 0, 1, 10, 161), new TimeSpan(0, 0, 1, 24, 161), Team.teams[1], Team.teams[2], Team.teams[3]);
                DebugAddUserAndSeasonUser(seasons[0], "slow", new TimeSpan(0, 0, 1, 15, 255), new TimeSpan(0, 0, 1, 30, 255), Team.teams[1], Team.teams[2], Team.teams[3]);
                DebugAddUserAndSeasonUser(seasons[0], "very fast", new TimeSpan(0, 0, 1, 9, 863), new TimeSpan(0, 0, 1, 23, 863), Team.teams[1], Team.teams[2], Team.teams[3]);
                DebugAddUserAndSeasonUser(seasons[0], "very very fast", new TimeSpan(0, 0, 1, 9, 351), new TimeSpan(0, 0, 1, 23, 351), Team.teams[1], Team.teams[2], Team.teams[3]);

                Debug.Log("Assigning drivers");
                seasons[0].AssignDrivers();

                for (int i = 0; i < seasons[0].races.Count; i++)
                {
                    seasons[0].races[i].results = new List<Race.Result>();
                    for (int j = 0; j < seasons[0].users.Count; j++)
                    {
                        seasons[0].races[i].results.Add(new Race.Result(seasons[0].users[j].user, j + 1, new TimeSpan(0, 0, 1, 25, 255), new TimeSpan(0,0,5,50,500)));
                    }
                }

                UpdateSeasonSql(seasons[0]);

                seasons[0].Log();
            }
        }

        void DebugAddUserAndSeasonUser(Season season, string login, TimeSpan lapDry, TimeSpan lapWet, Team team1, Team team2, Team team3, string steam = "")
        {
            User u = CreateUser(login, "", "", steam, "", false, "");
            RegisterUser(u);

            AddSeasonUser(season, u, lapDry, lapWet, "", "", team1, team2, team3);
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
                new MSQL.Column("ip", MSQL.ColumnType.STRING),
                new MSQL.Column("admin", MSQL.ColumnType.BOOLEAN),
                new MSQL.Column("donate", MSQL.ColumnType.INT)
            });

            if (objects.Count == 0) return;

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
                new MSQL.Column("lapdry", MSQL.ColumnType.TIME, false),
                new MSQL.Column("lapwet", MSQL.ColumnType.TIME, false),
                new MSQL.Column("lapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("lapwetlink", MSQL.ColumnType.STRING),
                new MSQL.Column("priority", MSQL.ColumnType.INT),
                new MSQL.Column("team", MSQL.ColumnType.INT),
                new MSQL.Column("pteam1", MSQL.ColumnType.INT),
                new MSQL.Column("pteam2", MSQL.ColumnType.INT),
                new MSQL.Column("pteam3", MSQL.ColumnType.INT)

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

        bool LoadSeasonRaceResultsFromDB(Race race)
        {
            if(race.resultsTable == null || race.resultsTable.Length == 0)
            {
                Debug.LogWarning("[WindWingAppServer.LoadSeasonRaceResultsFromDB] ResultsTable for race " + race.id.ToString() + " is null or empty");
                return true;
            }

            if(!sql.TableExists(race.resultsTable))
            {
                Debug.LogWarning("[WindWingAppServer.LoadSeasonRaceResultsFromDB] ResultsTable for race " + race.id.ToString() + " doesn't exists, table name: \"" + race.resultsTable + "\"");
                return false;
            }

            List<object[]> oObjects = sql.ReadEntries(race.resultsTable, new List<MSQL.Column>
            {
                new MSQL.Column("user", MSQL.ColumnType.INT),
                new MSQL.Column("place", MSQL.ColumnType.INT),
                new MSQL.Column("bestlap", MSQL.ColumnType.TIME, false),
                new MSQL.Column("time", MSQL.ColumnType.TIME, false),
                new MSQL.Column("dnf", MSQL.ColumnType.BOOLEAN),
                new MSQL.Column("started", MSQL.ColumnType.BOOLEAN)
            });

            List<object[]> objects = MUtil.FlipSQLData(oObjects);
            for(int i = 0;i<objects.Count;i++)
            {
                var r = new Race.Result(objects[i]);
                if(!r.good)
                {
                    return false;
                }
                race.results.Add(r);
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

                LoadSeasonRaceResultsFromDB(newRace);
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
                    new MSQL.Column("timetrackid", MSQL.ColumnType.INT),
                    new MSQL.Column("assigned", MSQL.ColumnType.BOOLEAN)
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
            /*if (season == 1)
            {
                return "25{(Quorthon,162,RDB);(Minik,127,MCL);(kypE,134,RDB);(BARTEQ,75,HAS);(Skomek,80,TRS);(Rogar2630,61,FER);(Patryk913,84,TRS);(Giro,68,ARM);(Yomonoe,44,MCL);(Copy JR,39,REN);(R4zor,37,MER);(slepypirat,34,RPT);(koczejk,26,HAS);(Shiffer,26,RPT);(cichy7220,23,MER);(Allu,21,OTH);(Myslav,14,OTH);(Hokejode,11,ARM);(Paw3lo,10,OTH);(Lewandor,16,OTH);(xVenox,1,OTH);(Kamilos61,0,REN);(Grok12,-1,FER);([SOL]NikoMon,-2,OTH);(Bany,-2,OTH)}";
            }
            else
            {

                try
                {
                    var s = GetSeason(season);
                    return s.GetLeaderboards();
                }
                catch (Exception e)
                {
                    Debug.Exception(e, "[WindWingAppServer.GenerateLeaderboardsString]");
                    return "";
                }
            }*/

            try
            {
                var s = GetSeason(season);
                return s.GetLeaderboards();
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[WindWingAppServer.GenerateLeaderboardsString]");
                return "";
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
        public WindWingAppServer(bool clearAllData = false, string additionalCmds = "")
        {
            File.AppendAllText(Debug.GetLogPath(), "[" + DateTime.Now.ToString() + "]\n");

            if(File.Exists(Path.Combine(Debug.GetLogPath(false), "appversion.ini")))
            {
                try
                {
                    string data = File.ReadAllText(Path.Combine(Debug.GetLogPath(false), "appversion.ini"));
                    appLatestVersion = int.Parse(data);
                }
                catch(Exception e)
                {
                    Debug.Exception(e, "Cannot load app version");
                }
            }
            else
            {
                Debug.LogWarning("App latest version file not found, path: " + Path.Combine(Debug.GetLogPath(false), "appversion.ini"));
            }


            Debug.Log("Loading WindWingAppServer version " + appVersion + "...");

            if(MUtil.debug)
            {
                Debug.Log("[WindWingAppServer] DEBUG MODE ACTIVE", ConsoleColor.Magenta);
            }

            sql = new MSQL("localhost", "WindWingApp", "windWingStrongPass", "WindWingApp");

            if(clear || clearAllData)
            {
                sql.DropAllTables();
                Debug.Log("Clearing complete");
            }

            if(additionalCmds == "-addDNF")
            {
                string[] lines = sql.ExecuteCommand("SHOW TABLES;");
                Debug.Log("-addDNF");
                for(int i = 0;i<lines.Length;i++)
                {
                    //Debug.Log(lines[i]);
                    if(lines[i].Contains("_results_"))
                    {
                        sql.ExecuteCommand("ALTER TABLE " + lines[i] + " ADD COLUMN dnf BOOLEAN NOT NULL");
                        sql.ExecuteCommand("ALTER TABLE " + lines[i] + " ADD COLUMN started BOOLEAN NOT NULL");
                    }
                }
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

        public bool ResetPassword(User u, string token, string password)
        {
            bool good = false;

            try
            {

                workingWithPasses = true;
                for (int i = 0; i < resetPasses.Count; i++)
                {
                    if (resetPasses[i].user.id == u.id && resetPasses[i].token == token)
                    {
                        u.password = password;
                        resetPasses.RemoveAt(i);
                        good = true;
                        break;
                    }
                }

                RewriteUser(u);
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[ResetPassword]");
            }
            workingWithPasses = false;

            return good;
        }

        public ResetPass AddPasswordReset(User u)
        {
            while(workingWithPasses)
            {
                Thread.Sleep(10);
            }
            workingWithPasses = true;

            ResetPass pass = new ResetPass(u);
            resetPasses.Add(pass);

            workingWithPasses = false;

            return pass;
        }

        void ParseCommand(string command)
        {
            try
            {
                string[] parts = command.Split(' ');

                bool ok = true;
                parts[0] = parts[0].ToLower();
                switch (parts[0])
                {
                    case "?":
                    case "help":
                    case "commands":
                    case "cmd":

                        Debug.Log("- setAdmin {user} {1/0}");
                        Debug.Log("- getLogLocation");
                        Debug.Log("- addPassToken {user}");
                        Debug.Log("- setDoante {user} {value}");
                        Debug.Log("- assignDrivers {season}");
                        Debug.Log("- logSeason {season}");
                        break;

                    case "debugdrivers":
                        {
                            break;
                        }

                    case "logseason":
                        {
                            var season = GetSeason(int.Parse(parts[1]));
                            season.Log();
                            break;
                        }

                    case "assigndrivers":
                        {
                            var season = GetSeason(int.Parse(parts[1]));
                            season.AssignDrivers();

                            break;
                        }
                        

                    case "setadmin":
                        {
                            User u = GetUser(parts[1]);

                            if (parts.Length > 2 && parts[2] == "0")
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
                        }

                    case "getloglocation":
                        Debug.Log(Debug.GetLogPath());
                        break;

                    case "addpasstoken":
                        {
                            User u = GetUser(parts[1]);
                            var tok = AddPasswordReset(u);

                            Debug.Log("Added reset token \"" + tok.token + "\" to " + u.login);
                            break;
                        }
                    case "setdonate":
                        {
                            User u = GetUser(parts[1]);
                            if(u == null)
                            {
                                Debug.LogError("User not found");
                                ok = false;
                                break;
                            }
                            int val = int.Parse(parts[2]);

                            u.donate = val;
                            RewriteUser(u);

                            break;
                        }

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
