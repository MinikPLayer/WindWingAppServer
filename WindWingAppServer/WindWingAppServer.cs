using System;
using System.Collections.Generic;
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

            public Race()
            {

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

                public SeasonUser(User user)
                {
                    this.user = user;
                }

                public SeasonUser(User user, TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink)
                {
                    this.user = user;

                    FillVariables(lapDry, lapWet, lapDryLink, lapWetLink);
                }

                public void FillVariables(TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink)
                {
                    this.lapDry = lapDry;
                    this.lapWet = lapWet;
                    this.lapDryLink = lapDryLink;
                    this.lapWetLink = lapWetLink;
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
                }

                public List<MSQL.Value> ToSqlValues()
                {
                    return new List<MSQL.Value>(){
                    new MSQL.Value("id", user.id),
                    new MSQL.Value("lapdry", lapDry),
                    new MSQL.Value("lapwet", lapWet),
                    new MSQL.Value("lapdrylink", lapDryLink),
                    new MSQL.Value("lapwetlink", lapWetLink),
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

            public Season(int id, int racesCount, bool finished, string prefix, List<Race> races = null, RegistrationData registrationData = null)
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
                    registrationData = new RegistrationData(false, this.id);
                }

                this.registrationData = registrationData;
            }
        }

        public class RegistrationData
        {
            public bool opened;
            public int season;

            public RegistrationData(bool opened, int season)
            {
                this.opened = opened;
                this.season = season;
            }

            public override string ToString()
            {
                return "(" + (opened ? "O" : "C") + "," + season.ToString() + ")";
            }
        }

        public Track[] tracks = new Track[] {
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

        public Team[] teams = new Team[] {
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

        //      PLACEHOLDER     
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

        public User CreateUser(string login, string password, string email, string steam, string ip, bool admin = false, string token = "")
        {
            if(token.Length == 0)
            {
                token = GenerateToken();
            }

            return new User(users.Count, login, password, token, email, steam, ip, admin);
        }

        bool WriteSeasonTable(int season, int usersCount, int racesCount, bool finished, bool overwrite = false)
        {
            Debug.Log("Seasons found: ");
            object[] ids = sql.ReadEntry("seasons", new MSQL.Column("id", MSQL.ColumnType.INT)); //new List<string>() { "id" }, new List<MSQL.ColumnType>() { MSQL.ColumnType.INT });
            for(int i = 0;i<ids.Length;i++)
            {

                if((int)ids[i] == season)
                {
                    if (overwrite)
                    {
                        Debug.LogWarning("[WWAS.WriteSeasonTable] Season with id " + season + " already found");
                        sql.RemoveEntry("seasons", new MSQL.Value("id", season));
                    }
                    else
                    {
                        Debug.LogError("[WWAS.WriteSeasonTable] Season with id " + season + " already found");
                        return false;
                    }
                }
            }

            string finishedStr = finished ? "1" : "0";
            sql.AddEntry("seasons", new List<MSQL.Value>() {
                new MSQL.Value("id", season.ToString()),
                new MSQL.Value("prefix", "s" + season.ToString() + "_"),
                new MSQL.Value("racescount", racesCount),
                new MSQL.Value("finished", finished)
            });

            int count = sql.ReadEntry<int>("dbinfo", new MSQL.Column("seasonsCount", MSQL.ColumnType.INT))[0];
            sql.ModifyEntry("dbinfo", new MSQL.Value("seasonsCount", count + 1));

            sql.CreateTable("s" + season.ToString(), new List<MSQL.Column>() {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("trackid",  MSQL.ColumnType.INT),
                new MSQL.Column("completed",  MSQL.ColumnType.BOOLEAN),
                new MSQL.Column("registrationend", MSQL.ColumnType.DATETIME),
                new MSQL.Column("racedate",  MSQL.ColumnType.DATETIME),
                new MSQL.Column("resultstable", MSQL.ColumnType.STRING),
                new MSQL.Column("timetrackid", MSQL.ColumnType.INT)
            });

            sql.CreateTable("s" + season.ToString() + "_users", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("lapdry", MSQL.ColumnType.TIME),
                new MSQL.Column("lapwet", MSQL.ColumnType.TIME),
                new MSQL.Column("lapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("lapwetlink", MSQL.ColumnType.STRING)
            });

            return true;
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

        void CreateDBStructure()
        {
            sql.CreateTable("dbinfo", new List<MSQL.Column>() { new MSQL.Column("version", MSQL.ColumnType.INT), new MSQL.Column("seasonsCount", MSQL.ColumnType.INT) });
            //sql.AddEntry("dbinfo", new List<string>() { "version", "seasonsCount" }, new List<string>() { version.ToString(), "0" });
            sql.AddEntry("dbinfo", new List<MSQL.Value>()
            {
                new MSQL.Value("version", version.ToString()),
                new MSQL.Value("seasonsCount", "0")
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
                new MSQL.Column("prefix", MSQL.ColumnType.STRING)
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
            //WriteSeason1Table();
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
                //AddUser(CreateUser(objects[i]));
                User u = CreateUser(uObjects[i]);
                Debug.Log("Loaded user " + u.id + ": \"" + u.login + "\" with password: \"" + u.password + "\"");
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

        const bool clear = false;
        public WindWingAppServer(bool clearAllData = false)
        {
            Debug.Log("Loading WindWingAppServer version 0.3a...");

            sql = new MSQL("localhost", "WindWingApp", "windWingStrongPass", "WindWingApp");

            if(clear || clearAllData)
            {
                sql.DropAllTables();
            }


            bool exists = sql.TableExists("dbinfo");
            Console.WriteLine("Table dbinfo exists: " + exists);
            if (exists)
            {
                GetDBData();
            }
            else
            {
                CreateDBStructure();
            }

            networkData = new NetworkData(this, 8148);

            while(true)
            {
                //Thread.Sleep(10);
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

                switch (parts[0])
                {
                    case "setadmin":
                        User u = GetUser(parts[1]);
                        u.admin = true;
                        //RewriteUser(u);
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
                        break;
                }

                Debug.Log("OK");
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[Command] Parsing exception: ");
            }
        }
    }
}
