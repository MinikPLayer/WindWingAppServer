using System;
using System.Collections.Generic;
using System.Text;

namespace WindWingAppServer.Models
{
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

        public string Serialize()
        {
            return "track{id(" + id + ")}";
        }

        public List<string> ToSQL()
        {
            return new List<string>() { id.ToString(), name, country, city, length.ToString(), record.ToString() };
        }

        public static Track GetTrack(string country)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].country == country)
                {
                    return tracks[i];
                }
            }

            return null;
        }

        public static Track GetTrack(int id)
        {
            if (id < 0 || id >= tracks.Length) return null;
            return tracks[id];
        }

        public static List<List<string>> TracksToSQL()
        {
            List<List<string>> data = new List<List<string>>();
            for (int i = 0; i < tracks.Length; i++)
            {
                data.Add(tracks[i].ToSQL());
            }
            return data;
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
    }
}
