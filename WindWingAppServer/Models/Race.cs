using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindWingAppServer.Models
{
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
            this.track = Track.GetTrack(0);
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
            Deserialize(raceString);
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
            return "race{" + track.Serialize() + ",date{" + date.ToString(new CultureInfo("de-DE")) + "}}";
        }


        public Race(int trackID, DateTime date, List<Result> results = null)
        {
            this.track = Track.GetTrack(trackID);
            this.date = date;

            this.results = results;
        }

        public Race(List<Result> results)
        {
            this.results = results;
        }
    }
}
