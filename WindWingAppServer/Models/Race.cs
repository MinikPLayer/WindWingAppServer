using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindWingAppServer.Models
{
    public class Race
    {
        public int id;
        public DateTime date;
        public Track track;
        public string resultsTable;

        public class Result
        {
            public User user;
            public int place;
            public TimeSpan bestLap;
            public TimeSpan time;
            public bool dnf;
            public bool started;

            public bool good = true;

            public Result(User user, int place, TimeSpan bestLap, TimeSpan time, bool dnf = false, bool started = false)
            {
                this.user = user;
                this.place = place;
                this.bestLap = bestLap;
                this.time = time;
                this.dnf = dnf;
                this.started = started;
            }

            public Result(object[] data)
            {
                good = LoadFromSql(data);
            }

            public bool LoadFromSql(object[] data)
            {
                try
                {
                    if (data.Length < 6)
                    {
                        Debug.LogError("[Race.Result.LoadFromSql] Not enough data to load from, found only " + data.Length + " columns");
                        return false;
                    }

                    user = User.GetUser((int)data[0]);
                    place = (int)data[1];
                    bestLap = (TimeSpan)data[2];
                    time = (TimeSpan)data[3];
                    dnf = (bool)data[4];
                    started = (bool)data[5];

                    return true;
                }
                catch (Exception e)
                {
                    Debug.Exception(e, "[Race.LoadFromSql]");
                    return false;
                }
            }
        }

        public List<Result> results = new List<Result>();

        public void LoadDefaults()
        {
            this.id = -1;
            this.date = DateTime.MinValue;
            this.track = Track.GetTrack(0);

            this.resultsTable = "";
        }

        public bool LoadFromSql(object[] data)
        {
            try
            {
                if (data.Length < 4)
                {
                    Debug.LogError("[Race.LoadFromSql] Not enough data to load from, found only " + data.Length + " columns");
                    return false;
                }

                id = (int)data[0];
                track = Track.GetTrack((int)data[1]);
                date = (DateTime)data[2];
                resultsTable = (string)data[3];

                return true;
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[Race.LoadFromSql]");
                return false;
            }
        }

        public void Log()
        {
            Debug.Log("Race id: " + id.ToString());
            Debug.Log("Date: " + date.ToString(new CultureInfo("de-DE")));
            Debug.Log("Track: " + track.country);
            Debug.Log("Results count: " + results.Count);
        }

        public Race()
        {
            LoadDefaults();
        }

        public Race(string raceString)
        {
            LoadDefaults();
            Deserialize(raceString);
        }

        public Race(int id, int trackID, DateTime date, List<Result> results = null, string resultsTable = "")
        {
            this.id = id;
            this.track = Track.GetTrack(trackID);
            this.date = date;

            this.results = results;

            this.resultsTable = resultsTable;
        }

        public Race(int id, List<Result> results, string resultsTable = "")
        {
            this.id = id;
            this.results = results;

            this.resultsTable = resultsTable;
        }

        bool ParseSinglePacket(string header, string content)
        {
            try
            {
                switch (header)
                {
                    case "id":
                        id = int.Parse(content);
                        return true;

                    case "track":
                        if (content.StartsWith("c("))
                        {
                            content = content.Substring(0, content.Length - 1).Remove(0, 2); // remove c( and )
                            track = Track.GetTrack(content);

                            return true;
                        }
                        else if (content.StartsWith("id("))
                        {
                            content = content.Substring(0, content.Length - 1).Remove(0, 3); // remove id( and )
                            track = Track.GetTrack(int.Parse(content));

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

                    case "results":
                        {
                            List<string> data = MUtil.SplitWithBrackets(content);
                            results.Clear();
                            for(int i = 0;i<data.Count;i++)
                            {
                                string[] info = data[i].Split('|');
                                if(info.Length < 4)
                                {
                                    Debug.LogError("[Race.ParseSinglePacket] Not enough data in results packet");
                                    return false;
                                }

                                for(int j = 0;j<info.Length;j++)
                                {
                                    Debug.Log("Info[" + j.ToString() + "]: " + info[j]);
                                }

                                User u = null;
                                int place = int.Parse(info[0]);
                                int id = int.Parse(info[1]);
                                TimeSpan gap = TimeSpan.ParseExact(info[2], "hh':'mm':'ss':'fff", null);
                                TimeSpan bestLap = TimeSpan.ParseExact(info[3], "mm':'ss':'fff", null);
                                bool dnf = false;
                                bool started = false;

                                if(info.Length > 5)
                                {
                                    dnf = bool.Parse(info[4]);
                                    started = bool.Parse(info[5]);
                                }

                                for (int j = 0;j<User.users.Count;j++)
                                {
                                    if(User.users[j].id == id)
                                    {
                                        u = User.users[j];
                                        break;
                                    }
                                }
                                if(u == null)
                                {
                                    Debug.LogError("[Race.ParseSinglePacket] User with id " + id.ToString() + " not found");
                                    return false;
                                }

                                var res = new Result(u, place, bestLap, gap, dnf, started);
                                results.Add(res);
                                Debug.Log("Added result for race " + this.track.country.ToString() + " for user " + u.login);
                                
                            }
                            return true;
                        }

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

        public bool Deserialize(string str)
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

        public string Serialize()
        {
            string str = "race{id{" + id.ToString() + "}," + track.Serialize() + ",date{" + date.ToString(new CultureInfo("de-DE")) + "}";
        
            if(results.Count > 0)
            {
                str += ",results{";

                for(int i = 0;i<results.Count;i++)
                {
                    str += results[i].place.ToString() + "|" + results[i].user.id.ToString() + "|" + results[i].time.ToString("hh':'mm':'ss':'fff") + "|" + results[i].bestLap.ToString("mm':'ss':'fff") + "|" + results[i].dnf.ToString() + "|" + results[i].started.ToString();
                    if(i != results.Count - 1)
                    {
                        str += ',';
                    }
                }

                str += "}";
            }

            str += "}";

            return str;
        }

    }
}
