using System;
using System.Collections.Generic;
using System.Text;

namespace WindWingAppServer.Models
{
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

            public SeasonUser(User user, TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink, int priority = int.MaxValue)
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

        public bool good;
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
            for (int i = 0; i < races.Count; i++)
            {
                races[i].Log();
            }
        }

        public Season(string seasonString, int forceID = 0)
        {
            SetDefaultValues();

            if (forceID > 0)
            {
                this.id = forceID;
            }
            good = Deserialize(seasonString);

            if(prefix.Length == 0)
            {
                prefix = "s" + this.id + "_";
            }
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

                    case "finished":
                        finished = bool.Parse(content);
                        return true;

                    case "registration":
                        registrationData.Deserialize(header + "{" + content + "}");
                        return true;

                    case "track":
                        if (content.StartsWith("c("))
                        {
                            content = content.Substring(0, content.Length - 1).Remove(0, 2); // remove c( and )
                            registrationTrack = Track.GetTrack(content);

                            return true;
                        }
                        else if (content.StartsWith("id("))
                        {
                            content = content.Substring(0, content.Length - 1).Remove(0, 3); // remove id( and )
                            registrationTrack = Track.GetTrack(int.Parse(content));

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
                        for (int i = 0; i < packets.Count; i++)
                        {
                            Race r = new Race();
                            if (!r.Deserialize(packets[i]))
                            {
                                Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Cannot parse race packet");
                                return false;
                            }
                            races.Add(r);
                        }
                        racesCount = races.Count;
                        return true;

                    default:
                        Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Unknown header");
                        return false;
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Error parsing packet, exception: " + e.ToString());
                return false;
            }
        }

        public bool Deserialize(string str)
        {
            try
            {
                Debug.Log("Season string: " + str);

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
                Debug.Exception(e, "[WindWingAppServer.ParseSeasonString]");
                return false;
            }
        }

        public string Serialize()
        {
            string str = "Season{";

            str += "id{" + id.ToString() + "},";
            str += registrationData.Serialize() + ",";
            str += registrationTrack.Serialize() + ",";
            str += "finished{" + finished + "}";

            if (races.Count > 0)
            {
                str += ",races{";

                for (int i = 0; i < races.Count; i++)
                {
                    str += races[i].Serialize();
                    if (i != races.Count - 1)
                    {
                        str += ',';
                    }
                }

                str += "}";
            }

            return str + "}";
        }

        void SetDefaultValues()
        {
            this.id = 0;
            this.racesCount = 0;
            this.finished = false;
            this.prefix = "";

            this.races = new List<Race>(racesCount);

            registrationData = new RegistrationData(false, DateTime.MinValue);

            registrationTrack = Track.GetTrack(0);

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

            if (registrationData == null)
            {
                registrationData = new RegistrationData(false, DateTime.MinValue);
            }

            this.registrationData = registrationData;

            this.registrationTrack = registrationTrack;
        }
    }
}
