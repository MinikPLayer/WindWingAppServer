using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


using System.Threading;

namespace WindWingAppServer
{
    public class WindWingAppServer
    {
        MSQL sql;
        NetworkData networkData;

        const int version = 1;

        public class Track
        {
            public int id;
            public string name;
            public string country;
            public string city;
            public int length;
            public TimeSpan record;

            public Track(int id, string name, string country, string city, int length)
            {
                this.id = id;
                this.name = name;
                this.country = country;
                this.city = city;
                this.length = length;

                this.record = new TimeSpan(0, 0, 0, 0, 0);
            }

            public Track(int id, string name, string country, string city, int length, TimeSpan record)
            {
                this.id = id;
                this.name = name;
                this.country = country;
                this.city = city;
                this.length = length;

                this.record = record;
            }

            public List<string> ToSQL()
            {
                return new List<string>() { id.ToString(), name, country, city, length.ToString(), record.ToString() };
            }
        }

        public class Team
        {
            public int id;
            public string name;
            public string shortName;
            public string iconPath;
            

            public Team(int id, string name, string shortName, string iconPath)
            {
                this.id = id;
                this.name = name;
                this.shortName = shortName;
                this.iconPath = iconPath;
            }

            internal List<string> ToSQL()
            {
                return new List<string>() { id.ToString(), name, shortName, iconPath };
            }
        }

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
                if(data.Length < 5)
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

        public class Race
        {
            public DateTime date;
            public Track track;

            public class Result
            {
                public User user;
                public int place;

                public Result(User user, int place)
                {
                    this.user = user;
                    this.place = place;
                }
            }

            public List<Result> results = new List<Result>();

            public void LoadDefaults()
            {
                this.date = DateTime.MinValue;
                this.track = GetTrack(0);
            }

            public void Log()
            {
                Debug.Log("Date: " + date.ToString(new CultureInfo("de-DE")));
                Debug.Log("Track: " + track.country);
            }

            public Race()
            {
                LoadDefaults();
            }

            public Race(string raceString)
            {
                LoadDefaults();
                ParseRaceString(raceString);
            }

            bool ParseSinglePacket(string header, string content)
            {
                try
                {
                    switch (header)
                    {
                        case "track":
                            if (content.StartsWith("c("))
                            {
                                content = content.Substring(0, content.Length - 1).Remove(0, 2); // remove c( and )
                                track = GetTrack(content);

                                return true;
                            }
                            else if (content.StartsWith("id("))
                            {
                                content = content.Substring(0, content.Length - 1).Remove(0, 3); // remove id( and )
                                track = GetTrack(int.Parse(content));

                                return true;
                            }
                            else
                            {
                                Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Unknown track header");
                            }
                            return false;

                        case "date":
                            date = DateTime.Parse(content, new CultureInfo("de-DE"));
                            return true;

                        default:
                            Debug.LogError("[WindWingApp.Season.Race.ParseSinglePacket] Unknown header");
                            return false;
                    }
                }
                catch (Exception e)
                {
                    Debug.Exception(e, "[WindWingAppServer.Season.Race.ParseSeasonString]");
                    return false;
                }
            }

            public bool ParseRaceString(string str)
            {
                try
                {
                    if (!str.StartsWith("race{"))
                    {
                        Debug.LogError("[Season.Race.ParseRaceString] It's no a race packet, bad magic");
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
                                    Debug.LogError("[WindWingApp.Season.ParseSeasonString] Cannot parse packet with header: " + packets[i].Substring(0, j));
                                    return false;
                                }
                                done = true;
                                break;
                            }
                        }
                        if (!done)
                        {
                            Debug.LogError("[WindWingApp.Season.ParseSeasonString] Cannot find a closing bracket, incomplete packet");
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Debug.Exception(e, "[WindWingAppServer.Season.Race.ParseSeasonString]");
                    return false;
                }
            }

            public Race(int trackID, DateTime date, List<Result> results = null)
            {
                this.track = GetTrack(trackID);
                this.date = date;

                this.results = results;
            }

            public Race(List<Result> results)
            {
                this.results = results;
            }
        }

        public class Season
        {
            public class SeasonUser
            {
                public User user;
                public TimeSpan lapDry;
                public TimeSpan lapWet;
                public string lapDryLink;
                public string lapWetLink;
                public int priority;

                public SeasonUser(User user)
                {
                    this.user = user;
                }

                public SeasonUser(User user, TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink,int priority = int.MaxValue)
                {
                    this.user = user;

                    FillVariables(lapDry, lapWet, lapDryLink, lapWetLink, priority);
                }

                public void FillVariables(TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink, int priority = int.MaxValue)
                {
                    this.lapDry = lapDry;
                    this.lapWet = lapWet;
                    this.lapDryLink = lapDryLink;
                    this.lapWetLink = lapWetLink;
                    this.priority = priority;
                }

                public void LoadFromSql(object[] data)
                {
                    if (data.Length < 5)
                    {
                        Debug.LogError("[User.LoadFromSqlSeason] Not enough data to load from, found only " + data.Length + " columns");
                    }

                    lapDry = (TimeSpan)data[1];
                    lapWet = (TimeSpan)data[2];
                    lapDryLink = (string)data[3];
                    lapWetLink = (string)data[4];
                    priority = (int)data[5];
                }

                public List<MSQL.Value> ToSqlValues()
                {
                    return new List<MSQL.Value>(){
                        new MSQL.Value("id", user.id),
                        new MSQL.Value("lapdry", lapDry),
                        new MSQL.Value("lapwet", lapWet),
                        new MSQL.Value("lapdrylink", lapDryLink),
                        new MSQL.Value("lapwetlink", lapWetLink),
                        new MSQL.Value("priority", priority)
                    };
                }
            }

            public int id;
            public int racesCount;
            public List<Race> races;
            public List<SeasonUser> users = new List<SeasonUser>();
            public RegistrationData registrationData;
            public bool finished;
            public string prefix;
            public Track registrationTrack;

            public void Log()
            {
                Debug.Log("Season " + id.ToString() + ": ");
                Debug.Log("Races count: " + racesCount);
                Debug.Log("Finished: " + finished);
                if (registrationTrack == null)
                {
                    Debug.Log("Registration track: null");
                }
                else
                {
                    Debug.Log("Registration track: " + registrationTrack.country);
                }


                Debug.Log("Races (" + races.Count.ToString() + "): ");
                for(int i = 0;i<races.Count;i++)
                {
                    races[i].Log();
                }
            }

            public Season(string seasonString, int forceID = -1)
            {
                SetDefaultValues();

                if(forceID > 0)
                {
                    this.id = forceID;
                }
                ParseSeasonString(seasonString);
            }

            bool ParseSinglePacket(string header, string content)
            {
                try
                {
                    switch (header)
                    {
                        case "finished":
                            finished = bool.Parse(content);
                            return true;

                        case "track":
                            if(content.StartsWith("c("))
                            {
                                content = content.Substring(0, content.Length - 1).Remove(0, 2); // remove c( and )
                                registrationTrack = GetTrack(content);

                                return true;
                            }
                            else if(content.StartsWith("id("))
                            {
                                content = content.Substring(0, content.Length - 1).Remove(0, 3); // remove id( and )
                                registrationTrack = GetTrack(int.Parse(content));

                                return true;
                            }
                            else
                            {
                                Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Unknown track header");
                            }
                            return false;

                        case "races":
                            races.Clear();
                            List<string> packets = MUtil.SplitWithBrackets(content);
                            for(int i = 0;i<packets.Count;i++)
                            {
                                Race r = new Race();
                                if(!r.ParseRaceString(packets[i]))
                                {
                                    Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Cannot parse race packet");
                                    return false;
                                }
                                races.Add(r);
                            }
                            return true;

                        default:
                            Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Unknown header");
                            return false;
                    }

                }
                catch(Exception e)
                {
                    Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Error parsing packet, exception: " + e.ToString());
                    return false;
                }
            }

            public bool ParseSeasonString(string str)
            {
                try
                {
                    if (!str.StartsWith("Season{"))
                    {
                        Debug.LogError("[WindWingApp.Season.ParseSeasonString] Not a season string: bad magic");
                        return false;
                    }

                    str = str.Substring(0, str.Length - 1).Remove(0, 7); // Remove Season{ and } at the end

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
                                    Debug.LogError("[WindWingApp.Season.ParseSeasonString] Cannot parse packet with header: " + packets[i].Substring(0,j));
                                    return false;
                                }
                                done = true;
                                break;
                            }
                        }
                        if (!done)
                        {
                            Debug.LogError("[WindWingApp.Season.ParseSeasonString] Cannot find a closing bracket, incomplete packet");
                            return false;
                        }
                    }

                    return true;
                }
                catch(Exception e)
                {
                    Debug.Exception(e, "[WindWingAppServer.ParseSeasonString]");
                    return false;
                }
            }

            void SetDefaultValues()
            {
                this.id = -1;
                this.racesCount = 0;
                this.finished = false;
                this.prefix = "";

                this.races = new List<Race>(racesCount);

                registrationData = new RegistrationData(false, this.id, DateTime.MinValue);

                registrationTrack = GetTrack(0);
                
            }

            public Season(int id, int racesCount, bool finished, string prefix, Track registrationTrack, List<Race> races = null, RegistrationData registrationData = null)
            {
                this.id = id;
                this.racesCount = racesCount;
                this.finished = finished;
                this.prefix = prefix;

                if (races == null)
                {
                    this.races = new List<Race>(racesCount);
                }
                else
                {
                    this.races = races;
                }

                if(registrationData == null)
                {
                    registrationData = new RegistrationData(false, this.id, DateTime.MinValue);
                }

                this.registrationData = registrationData;

                this.registrationTrack = registrationTrack;
            }
        }

        public class RegistrationData
        {
            public bool opened;
            public int season;
            public DateTime endDate;

            public RegistrationData(bool opened, int season, DateTime endTime)
            {
                this.opened = opened;
                this.season = season;
                this.endDate = endTime;
            }

            public override string ToString()
            {
                return "(" + (opened ? "O" : "C") + "," + season.ToString() + ")";
            }
        }

        public static Track[] tracks = new Track[] {
                new Track(0, "Albert Park Circuit", "Australia", "Melbourne", 5303  ),
                new Track(1, "Bahrain International Circuit", "Bahrain", "Sakhir", 5412),
                new Track(2, "Shanghai International Circuit", "Chiny", "Shanghai", 5451 ),
                new Track(3, "Baku City Circuit", "Azerbejdzan", "Baku", 6003),
                new Track(4, "Circuit de Barcelona-Catalunya", "Hiszpania", "Montmelo", 4655 ),
                new Track(5, "Circuit de Monaco", "Monako", "Monako", 3337),
                new Track(6, "Circuit Gilles Villeneuve", "Kanada", "Montreal", 4361),
                new Track(7, "Circuit Paul Ricard", "Francja", "Le Castellet", 5842),
                new Track(8, "Red Bull Ring", "Austria", "Spielberg", 4318),
                new Track(9, "Silverstone", "Wielka Brytania", "Silverstone", 5891),
                new Track(10, "Hockenheimring", "Niemcy", "Hockenheim", 4574),
                new Track(11, "Hungaroring", "Wegry", "Mogyorod", 4381),
                new Track(12, "Circuit de Spa-Francorchamps", "Belgia", "Stavelot", 7004),
                new Track(13, "Autodromo Nationale Monza", "Wlochy", "Monza", 5793),
                new Track(14, "Marina Bay Street Circuit", "Singapur", "Singapur", 5063),
                new Track(15, "Sochi Autodrom", "Rosja", "Sochi", 5848),
                new Track(16, "Suzuka Circuit", "Japonia", "Suzuka", 5807),
                new Track(17, "Autódromo Hermanos Rodríguez", "Meksyk", "Mexico City", 4304),
                new Track(18, "Circuit of the Americas", "USA", "Austin", 5513),
                new Track(19, "Autódromo José Carlos Pace", "Brazylia", "Sao Paulo", 4309),
                new Track(20, "Yas Marina Circuit", "Zjedoczone Emiraty Arabskie", "Abu Dhabi", 5554)
        };

        public static Team[] teams = new Team[] {
            new Team(0, "Mercedes", "MER", ""),
            new Team(1, "Ferrari", "FER", ""),
            new Team(2, "Red Bull", "RDB", ""),
            new Team(3, "Renault", "REN", ""),
            new Team(4, "Haas", "HAS", ""),
            new Team(5, "McLaren", "MCL", ""),
            new Team(6, "Racing Point", "RPT", ""),
            new Team(7, "Alfa Romeo", "ARM", ""),
            new Team(8, "Toro Rosso", "TRS", ""),
            new Team(9, "Williams", "WIL", ""),
            new Team(10, "Other", "OTH", "")
        };

        public List<User> users = new List<User>();

        public List<Season> seasons = new List<Season>();

        int usersCount = 0;

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
            users.Add(user);
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
            users.Add(user);
        }

        public void AddSeason(Season s)
        {
            WriteSeasonTable(s);
            seasons.Add(s);
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
             
            string finishedStr = season.finished ? "1" : "0";
            sql.AddEntry("seasons", new List<MSQL.Value>() {
                new MSQL.Value("id", season.id.ToString()),
                new MSQL.Value("prefix", prefix),
                new MSQL.Value("racescount", season.racesCount),
                new MSQL.Value("finished", season.finished),
                new MSQL.Value("registrationend", season.registrationData.endDate),
                new MSQL.Value("timetrackid", season.registrationTrack.id)
            });

            int count = sql.ReadEntry<int>("dbinfo", new MSQL.Column("seasonsCount", MSQL.ColumnType.INT))[0];
            sql.ModifyEntry("dbinfo", new MSQL.Value("seasonsCount", count + 1));

            sql.CreateTable("s" + season.id.ToString(), new List<MSQL.Column>() {
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
                    sql.AddEntry("s" + season.id.ToString(), new List<MSQL.Value>
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

        public static Track GetTrack(string country)
        {
            for(int i = 0;i<tracks.Length;i++)
            {
                if(tracks[i].country == country)
                {
                    return tracks[i];
                }
            }

            return null;
        }

        public static Track GetTrack(int id)
        {
            /*for(int i = 0;i<tracks.Length;i++)
            {
                if(tracks[i].id == id)
                {
                    return tracks[i];
                }
            }
            return null;*/
            if (id < 0 || id >= tracks.Length) return null;
            return tracks[id];
        }

        List<List<string>> TracksToSQL()
        {
            List<List<string>> data = new List<List<string>>();
            for(int i = 0;i<tracks.Length;i++)
            {
                data.Add(tracks[i].ToSQL());
            }
            return data;
        }

        List<List<string>> TeamsToSQL()
        {
            List<List<string>> data = new List<List<string>>();
            for(int i = 0;i<teams.Length;i++)
            {
                data.Add(teams[i].ToSQL());
            }
            return data;
        }

        void FillTracksTable()
        {
            sql.AddEntries("tracks", new List<string>() { "id", "name", "country", "city", "tracklength", "trackrecord" }, TracksToSQL());
        }

        private void FillTeamsTable()
        {
            sql.AddEntries("teams", new List<string>() { "id", "name", "shortname", "iconpath" }, TeamsToSQL());
        }

        void WriteSeason1Data()
        {
            //WriteSeasonTable(1, 25, 12, true);

            Season season = new Season(1, 12, true, "S1_", GetTrack(0), new List<Race>
            {
                new Race(0, new DateTime(2020, 05, 04, 20,00,00)),
                new Race(1, new DateTime(2020, 05, 05, 20,00,00)),
                new Race(4, new DateTime(2020, 05, 06, 20,00,00)),
                new Race(6, new DateTime(2020, 05, 07, 20,00,00)),
                new Race(9, new DateTime(2020, 05, 08, 20,00,00)),
                new Race(12, new DateTime(2020, 05, 11, 20,00,00)),
                new Race(16, new DateTime(2020, 05, 12, 20,00,00)),
                new Race(17, new DateTime(2020, 05, 13, 20,00,00)),
                new Race(3, new DateTime(2020, 05, 14, 20,00,00)),
                new Race(18, new DateTime(2020, 05, 18, 20,00,00)),
                new Race(10, new DateTime(2020, 05, 19, 20,00,00)),
                new Race(11, new DateTime(2020, 05, 20, 20,00,00))
            }, new RegistrationData(false, 1, DateTime.MinValue));

            season.registrationTrack = GetTrack(0);

            WriteSeasonTable(season);

            seasons.Add(season);
        }

        void CreateDBStructure()
        {
            sql.CreateTable("dbinfo", new List<MSQL.Column>() { 
                new MSQL.Column("version", MSQL.ColumnType.INT), 
                new MSQL.Column("seasonsCount", MSQL.ColumnType.INT),
                new MSQL.Column("userscount", MSQL.ColumnType.INT)
            });
            //sql.AddEntry("dbinfo", new List<string>() { "version", "seasonsCount" }, new List<string>() { version.ToString(), "0" });
            sql.AddEntry("dbinfo", new List<MSQL.Value>()
            {
                new MSQL.Value("version", version.ToString()),
                new MSQL.Value("seasonsCount", "0"),
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
                new MSQL.Column("finished", MSQL.ColumnType.BOOLEAN),
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

            RegisterUser(CreateUser("Minik", "", "", "", "", true, ""));
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

            List<object[]> uObjects = new List<object[]>(objects[0].Length);
            for(int i = 0;i<objects[0].Length; i++)
            {
                object[] data = new object[objects.Count];
                for(int j = 0;j<objects.Count;j++)
                {
                    data[j] = objects[j][i];
                }
                uObjects.Add(data);
            }
            for(int i = 0;i<uObjects.Count;i++)
            {
                User u = CreateUser(uObjects[i]);
                AddUser(u);
            }
        }

        void GetDBData()
        {
            LoadUsersFromDB();
        }

        public string GenerateLeaderboardsString()
        {
            // Placeholder
            return "25{(Quorthon,162,RDB);(Minik,127,MCL);(kypE,134,RDB);(BARTEQ,75,HAS);(Skomek,80,TRS);(Rogar2630,61,FRI);(Patryk913,84,TRS);(Giro,68,ARO);(Yomonoe,44,MCL);(Copy JR,39,RNL);(R4zor,37,MER);(slepypirat,34,RPT);(koczejk,26,HAS);(Shiffer,26,RPT);(cichy7220,23,MER);(Allu,21,OTH);(Myslav,14,OTH);(Hokejode,11,ARO);(Paw3lo,10,OTH);(Lewandor,16,OTH);(xVenox,1,OTH);(Kamilos61,0,RNL);(Grok12,-1,FRI);([SOL]NikoMon,-2,OTH);(Bany,-2,OTH)}";
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
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].login.ToLower() == login)
                {
                    return users[i];
                }
            }
            return null;
        }

        public User GetUserCS(string login)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].login == login)
                {
                    return users[i];
                }
            }
            return null;
        }

        public User GetUserByMail(string email)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].email == email)
                {
                    return users[i];
                }
            }
            return null;
        }

        public User GetUserBySteam(string steam)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].steam == steam)
                {
                    return users[i];
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
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].login == login && users[i].token == token)
                {
                    return users[i];
                }
            }
            return null;
        }

        public User GetUserByTokenNCS(string login, string token)
        {
            login = login.ToLower();

            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].login.ToLower() == login && users[i].token == token)
                {
                    return users[i];
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
            for(int i = 0;i<users.Count;i++)
            {
                if(users[i].login == login && users[i].password == password)
                {
                    return users[i];
                }
            }
            return null;
        }

        public User GetUserNCS(string login, string password)
        {
            login = login.ToLower();
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].login.ToLower() == login && users[i].password == password)
                {
                    return users[i];
                }
            }
            return null;
        }

        const bool clear = true;
        public WindWingAppServer(bool clearAllData = false)
        {
            Debug.Log("Loading WindWingAppServer version 0.3b...");

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
                        u.admin = true;
                        if(parts.Length > 2 && parts[2] == "0")
                        {
                            sql.ExecuteCommand("UPDATE users SET admin=false WHERE id=" + u.id.ToString());
                        }
                        else
                        {
                            sql.ExecuteCommand("UPDATE users SET admin=true WHERE id=" + u.id.ToString());
                        }
                        
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
