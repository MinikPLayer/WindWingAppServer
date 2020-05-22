using System;
using System.Collections.Generic;
using System.Text;

namespace WindWingAppServer
{
    public class WindWingAppServer
    {
        MSQL sql;

        const int version = 1;

        public class Track
        {
            public int id;
            public string name;
            public string country;
            public string city;
            public int length;

            public Track(int id, string name, string country, string city, int length)
            {
                this.id = id;
                this.name = name;
                this.country = country;
                this.city = city;
                this.length = length;
            }

            public List<string> ToSQL()
            {
                return new List<string>() { id.ToString(), name, country, city, length.ToString() };
            }
        }

        public class User
        {
            public int id;
            public string login;
            public string password;
            public string steam;

            public TimeSpan bestLapDry;
            public TimeSpan bestLapWet;

            public string bestLapDryLink;
            public string bestLapWetLink;

            /// <summary>
            /// Registered after S1
            /// </summary>
            public bool newUserType;

            public bool admin = false;

            public User()
            {

            }

            public User(int id, string login, string password, string steam, TimeSpan bestLapDry, TimeSpan bestLapWet, string bestLapDryLink, string bestLapWetLink, bool admin = false)
            {
                FillVariables(id, login, password, steam, bestLapDry, bestLapWet, bestLapDryLink, bestLapWetLink, admin);
            }

            public User(int id, string login, string steam, bool admin = false)
            {
                this.id = id;
                this.login = login;
                this.steam = steam;
                this.admin = admin;
            }

            public void FillVariables(int id, string login, string password, string steam, TimeSpan bestLapDry, TimeSpan bestLapWet, string bestLapDryLink, string bestLapWetLink, bool admin = false)
            {
                this.id = id;
                this.login = login;
                this.password = password;
                this.steam = steam;
                this.bestLapDry = bestLapDry;
                this.bestLapWet = bestLapWet;
                this.bestLapDryLink = bestLapDryLink;
                this.bestLapWetLink = bestLapWetLink;

                this.admin = admin;

                newUserType = true;
            }

            public List<string> ToSQL()
            {
                return new List<string>() { id.ToString(), login, password, steam, bestLapDry.ToString(), bestLapWet.ToString(), bestLapDryLink, bestLapWetLink, admin ? "1" : "0" };
            }

            public List<MSQL.Value> ToSQLValues()
            {
                return new List<MSQL.Value>(){
                    new MSQL.Value("id", id),
                    new MSQL.Value("login", login),
                    new MSQL.Value("password", password),
                    new MSQL.Value("steam", steam),
                    new MSQL.Value("bestlapdry", bestLapDry),
                    new MSQL.Value("bestlapwet", bestLapWet),
                    new MSQL.Value("bestlapdrylink", bestLapDryLink),
                    new MSQL.Value("bestlapwetlink", bestLapWetLink),
                    new MSQL.Value("admin", admin)
                };
            }

            public void LoadFromSQL(object[] data)
            {
                if(data.Length < 9)
                {
                    Debug.LogError("[User.LoadFromSQL] Not enough data to load from, found only " + data.Length + " columns");
                    return;
                }

                id = (int)data[0];
                login = (string)data[1];
                password = (string)data[2];
                steam = (string)data[3];
                bestLapDry = (TimeSpan)data[4];
                bestLapWet = (TimeSpan)data[5];
                bestLapDryLink = (string)data[6];
                bestLapWetLink = (string)data[7];
                admin = (bool)data[8];
            }
        }

        Track[] tracks = new Track[] {
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

        List<User> users = new List<User>();

        public void RegisterUser(User user)
        {
            sql.AddEntry("users", user.ToSQLValues());
            users.Add(user);
        }

        void AddUser(User user)
        {
            users.Add(user);
        }

        User CreateUser(object[] data)
        {
            User u = new User();

            u.LoadFromSQL(data);

            return u;
        }

        User CreateUser(string login, string password, string steam, TimeSpan bestLapDry, TimeSpan bestLapWet, string bestLapDryLink, string bestLapWetLink)
        {
            return new User(users.Count, login, password, steam, bestLapDry, bestLapWet, bestLapDryLink, bestLapWetLink);
        }

        User CreateUser(string login, string steam)
        {
            return new User(users.Count, login, steam);
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
            //sql.AddEntry("seasons", new List<string>() { "id", "userscount", "racescount", "finished", "prefix" }, new List<string>() { season.ToString(), usersCount.ToString(), racesCount.ToString(), finishedStr});
            sql.AddEntry("seasons", new List<MSQL.Value>() {
                new MSQL.Value("id", season.ToString()),
                new MSQL.Value("prefix", "s" + season.ToString() + "_"),
                new MSQL.Value("userscount", usersCount),
                new MSQL.Value("racescount", racesCount),
                new MSQL.Value("finished", finished)
            });

            int count = sql.ReadEntry<int>("dbinfo", new MSQL.Column("seasonsCount", MSQL.ColumnType.INT))[0];
            sql.ModifyEntry("dbinfo", new MSQL.Value("seasonsCount", count + 1));

            sql.CreateTable("s" + season.ToString(), new List<MSQL.Column>() {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("trackid",  MSQL.ColumnType.INT),
                new MSQL.Column("completed",  MSQL.ColumnType.BOOLEAN),
                new MSQL.Column("racedate",  MSQL.ColumnType.DATETIME),
                new MSQL.Column("resultstable", MSQL.ColumnType.STRING)
            });

            return true;
        }

        List<List<string>> TracksToSql()
        {
            List<List<string>> data = new List<List<string>>();
            for(int i = 0;i<tracks.Length;i++)
            {
                data.Add(tracks[i].ToSQL());
            }
            return data;
        }

        void FillTracksTable()
        {
            sql.AddEntries("tracks", new List<string>() { "id", "name", "country", "city", "tracklength", "trackrecord" }, TracksToSql());
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
                new MSQL.Column("steam", MSQL.ColumnType.STRING),
                new MSQL.Column("bestlapdry", MSQL.ColumnType.TIME),
                new MSQL.Column("bestlapwet", MSQL.ColumnType.TIME),
                new MSQL.Column("bestlapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("bestlapwetlink", MSQL.ColumnType.STRING),
                new MSQL.Column("admin", MSQL.ColumnType.BOOLEAN)
            });


            CreateDBDebugStuff();

            FillTracksTable();
            //WriteSeason1Table();
        }

        void LoadUsersFromDB()
        {
            List<object[]> objects = sql.ReadEntries("users", new List<MSQL.Column>()
            {
                new MSQL.Column("id", MSQL.ColumnType.INT),
                new MSQL.Column("login", MSQL.ColumnType.STRING),
                new MSQL.Column("password", MSQL.ColumnType.STRING),
                new MSQL.Column("steam", MSQL.ColumnType.STRING),
                new MSQL.Column("bestlapdry", MSQL.ColumnType.TIME),
                new MSQL.Column("bestlapwet", MSQL.ColumnType.TIME),
                new MSQL.Column("bestlapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("bestlapwetlink", MSQL.ColumnType.STRING),
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

        public WindWingAppServer()
        {
            sql = new MSQL("localhost", "WindWingApp", "windWingStrongPass", "WindWingApp");

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

        }

        void CreateDBDebugStuff()
        {
            RegisterUser(CreateUser("TestUser", "123qwe", "https://steamcommunity.com/user/MinikPLayer2", new TimeSpan(0, 0, 1, 20, 000), new TimeSpan(0, 0, 1, 40, 000), "", ""));
            RegisterUser(CreateUser("SecondTestUser", "456rty", "https://steamcommunity.com/user/MinikPLayer", new TimeSpan(0, 0, 1, 18, 000), new TimeSpan(0, 0, 1, 42, 000), "", "")); 
        }
    }
}
