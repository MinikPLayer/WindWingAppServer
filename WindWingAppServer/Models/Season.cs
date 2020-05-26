using System;
using System.Collections.Generic;
using System.Globalization;
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

            public void LoadDefaults()
            {
                this.lapDry = new TimeSpan(0, 0, 0);
                this.lapWet = new TimeSpan(0, 0, 0);
                this.lapDryLink = "";
                this.lapWetLink = "";
                this.priority = int.MaxValue;
            }

            public void Log()
            {
                Debug.Log("Season user: " + user.login);
                Debug.Log("LapDry: " + lapDry + " - link: " + lapDryLink);
                Debug.Log("LapWet: " + lapWet + " - link: " + lapWetLink);
            }

            public SeasonUser()
            {
                LoadDefaults();
            }

            public SeasonUser(User user)
            {
                this.user = user;
            }

            public SeasonUser(User user, TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink, int priority = int.MaxValue)
            {
                this.user = user;

                FillVariables(lapDry, lapWet, lapDryLink, lapWetLink, priority);
            }

            bool ParseSinglePacket(string header, string content)
            {
                try
                {
                    switch (header)
                    {
                        case "id":
                            user = User.GetUser(int.Parse(content));
                            return true;

                        case "lapDry":
                            lapDry = TimeSpan.ParseExact(content, "mm':'ss':'fff", null);
                            return true;

                        case "lapWet":
                            lapWet = TimeSpan.ParseExact(content, "mm':'ss':'fff", null);
                            return true;

                        case "lapDryLink":
                            lapDryLink = content;
                            return true;

                        case "lapWetLink":
                            lapWetLink = content;
                            return true;

                        default:
                            Debug.LogError("[Season.SeasonUser.ParseSinglePacket] Unknown header");
                            return false;
                    }
                }
                catch (Exception e)
                {
                    Debug.Exception(e, "[Season.SeasonUser.ParseSinglePacket]");
                    return false;
                }
            }

            public bool Deserialize(string str)
            {
                try
                {
                    if (!str.StartsWith("seasonUser{"))
                    {
                        Debug.LogError("[Season.SeasonUser.Deserialize] It's no a race packet, bad magic");
                        return false;
                    }

                    str = str.Substring(0, str.Length - 1).Remove(0, 11); // remove race{ and }

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
                                    Debug.LogError("[Season.SeasonUser.Deserialize] Cannot parse packet with header: " + packets[i].Substring(0, j));
                                    return false;
                                }
                                done = true;
                                break;
                            }
                        }
                        if (!done)
                        {
                            Debug.LogError("[Season.SeasonUser.Deserialize] Cannot find a closing bracket, incomplete packet");
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Debug.Exception(e, "[Season.SeasonUser.Deserialize]");
                    return false;
                }
            }

            public string Serialize()
            {
                return "seasonUser{id{" + user.id.ToString() + "},lapDry{" + lapDry.ToString("mm':'ss':'fff") + "},lapWet{" + lapWet.ToString("mm':'ss':'fff") + "},lapDryLink{" + lapDryLink + "},lapWetLink{" + lapWetLink + "}}";
            }

            public void FillVariables(TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink, int priority = int.MaxValue)
            {
                this.lapDry = lapDry;
                this.lapWet = lapWet;
                this.lapDryLink = lapDryLink;
                this.lapWetLink = lapWetLink;
                this.priority = priority;
            }

            public bool LoadFromSql(object[] data)
            {
                try
                {
                    if (data.Length < 5)
                    {
                        Debug.LogError("[User.LoadFromSqlSeason] Not enough data to load from, found only " + data.Length + " columns");
                    }

                    user = User.GetUser((int)data[0]);
                    lapDry = (TimeSpan)data[1];
                    lapWet = (TimeSpan)data[2];
                    lapDryLink = (string)data[3];
                    lapWetLink = (string)data[4];
                    priority = (int)data[5];

                    return true;
                }
                catch(Exception e)
                {
                    Debug.Exception(e, "[Season.SeasonUser.LoadFromSql]");
                    return false;
                }
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
        public int finishedRaces;
        public bool finished
        {
            get
            {
                return finishedRaces >= racesCount;
            }
        }
        public string prefix;
        public Track registrationTrack;

        public bool good;
        public void Log()
        {
            Debug.Log("Season " + id.ToString() + ": ");
            Debug.Log("Races count: " + racesCount);
            Debug.Log("Finished races: " + finishedRaces);
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

            Debug.Log("Users (" + users.Count.ToString() + "): ");
            for(int i = 0;i<users.Count;i++)
            {
                users[i].Log();
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

                    case "finishedRaces":
                        finishedRaces = int.Parse(content);
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
                        {
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
                        }

                    case "users":
                        {
                            users.Clear();
                            List<string> packets = MUtil.SplitWithBrackets(content);
                            for (int i = 0; i < packets.Count; i++)
                            {
                                SeasonUser r = new SeasonUser();
                                if (!r.Deserialize(packets[i]))
                                {
                                    Debug.LogError("[WindWingApp.Season.ParseSinglePacket] Cannot parse race packet");
                                    return false;
                                }
                                users.Add(r);
                            }
                            racesCount = races.Count;
                            return true;
                        }

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
            str += "finishedRaces{" + finishedRaces + "}";

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

            if(users.Count > 0)
            {
                str += ",users{";

                for(int i = 0;i<users.Count;i++)
                {
                    str += users[i].Serialize();
                    if(i != users.Count - 1)
                    {
                        str += ',';
                    }
                }

                str += "}";
            }

            return str + "}";
        }

        public bool LoadFromSql(object[] data)
        {
            if (data.Length < 7)
            {
                Debug.LogError("[Season.LoadFromSql] Not enough data to load from, found only " + data.Length + " columns");
                return false;
            }

            try
            {

                id = (int)data[0];
                users = new List<SeasonUser>((int)data[1]); // Users Count
                racesCount = (int)data[2];
                races = new List<Race>(racesCount);
                finishedRaces = (int)data[3];
                prefix = (string)data[4];
                registrationData = new RegistrationData((DateTime)data[5]);
                registrationTrack = Track.GetTrack((int)data[6]);

                return true;
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[Season.LoadFromSql]");
                return false;
            }
        }

        void SetDefaultValues()
        {
            this.id = 0;
            this.racesCount = 0;
            this.finishedRaces = 0;
            this.prefix = "";

            this.races = new List<Race>(racesCount);

            registrationData = new RegistrationData(DateTime.MinValue);

            registrationTrack = Track.GetTrack(0);

        }

        public Season(object[] data)
        {
            good = LoadFromSql(data);
        }

        public Season(int id, int racesCount, int finishedRaces, string prefix, Track registrationTrack, List<Race> races = null, RegistrationData registrationData = null)
        {
            this.id = id;
            this.racesCount = racesCount;
            this.finishedRaces = finishedRaces;
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
                registrationData = new RegistrationData(DateTime.MinValue);
            }

            this.registrationData = registrationData;

            this.registrationTrack = registrationTrack;
        }
    }
}
