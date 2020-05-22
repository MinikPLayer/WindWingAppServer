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

            public List<string> ToSQLFormat()
            {
                return new List<string>() { id.ToString(), name, country, city, length.ToString() };
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

            return true;
        }

        void WriteSeason1Table()
        {
            WriteSeasonTable(1, 25, 12, true);

            /*sql.CreateTable("s1_results", new List<MSQL.Column>()
            {

            });*/
        }

        List<List<string>> TracksToSql()
        {
            List<List<string>> data = new List<List<string>>();
            for(int i = 0;i<tracks.Length;i++)
            {
                data.Add(tracks[i].ToSQLFormat());
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
                new MSQL.Column("bestlapdry", MSQL.ColumnType.INT),
                new MSQL.Column("bestlapwet", MSQL.ColumnType.INT),
                new MSQL.Column("bestlapdrylink", MSQL.ColumnType.STRING),
                new MSQL.Column("bestlapwetlink", MSQL.ColumnType.STRING)
            });

            FillTracksTable();
            WriteSeason1Table();
        }

        public WindWingAppServer()
        {
            sql = new MSQL("localhost", "WindWingApp", "windWingStrongPass", "WindWingApp");

            bool exists = sql.TableExists("dbinfo");
            Console.WriteLine("Table dbinfo exists: " + exists);
            if (exists)
            {
                sql.DropAllTables();
                //sql.CreateTable("users", new List<MSQL.Column>() { new MSQL.Column("login", MSQL.ColumnType.STRING), new MSQL.Column("password", MSQL.ColumnType.STRING), new MSQL.Column("email", MSQL.ColumnType.STRING) });
                CreateDBStructure();
            }
            else
            {
                //sql.CreateTable("users", new List<MSQL.Column>() { new MSQL.Column("login", MSQL.ColumnType.STRING), new MSQL.Column("password", MSQL.ColumnType.STRING), new MSQL.Column("email", MSQL.ColumnType.STRING) });
                CreateDBStructure();
            }

            //sql.AddEntry("users", new List<string>() { "login", "password", "email" }, new List<string>() { "user1", "pass1", "user1@gmail.com" });  //"login,password,email", "user1,pass1,user1@gmail.com");

        }
    }
}
