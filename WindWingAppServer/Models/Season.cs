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
            public Team team;
            public Team[] prefferedTeams;

            public void LoadDefaults()
            {
                this.lapDry = new TimeSpan(0, 0, 0);
                this.lapWet = new TimeSpan(0, 0, 0);
                this.lapDryLink = "";
                this.lapWetLink = "";
                this.priority = int.MaxValue;

                this.team = Team.GetTeam("Other");
                this.prefferedTeams = new Team[3];
                for(int i = 0;i<prefferedTeams.Length;i++)
                {
                    this.prefferedTeams[i] = Team.GetTeam("Other");
                }
            }

            public void Log()
            {
                Debug.Log("Season user: " + user.login);
                Debug.Log("LapDry: " + lapDry.ToString("mm':'ss':'fff") + " - link: " + lapDryLink);
                Debug.Log("LapWet: " + lapWet.ToString("mm':'ss':'fff") + " - link: " + lapWetLink);
                if(team != null)
                {
                    Debug.Log("Team: " + team.name);
                }
                Debug.Log("Preffered teams:\n\t1) " + prefferedTeams[0].name + "\n\t2) " + prefferedTeams[1].name + "\n\t3) " + prefferedTeams[2].name);
            }

            public SeasonUser()
            {
                LoadDefaults();
            }

            public SeasonUser(User user)
            {
                LoadDefaults();
                this.user = user;
            }

            public SeasonUser(User user, TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink, Team team1, Team team2, Team team3, int priority = int.MaxValue)
            {
                LoadDefaults();

                this.user = user;

                FillVariables(lapDry, lapWet, lapDryLink, lapWetLink, team1, team2, team3, priority);
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

                        case "team":
                            team = Team.GetTeam(int.Parse(content));
                            return true;

                        case "team1":
                            prefferedTeams[0] = Team.GetTeam(int.Parse(content));
                            return true;

                        case "team2":
                            prefferedTeams[1] = Team.GetTeam(int.Parse(content));
                            return true;

                        case "team3":
                            prefferedTeams[2] = Team.GetTeam(int.Parse(content));
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

            public enum AccessLevels
            {
                user,
                owner,
                admin
            }

            public string Serialize(AccessLevels accessLevel)
            {
                //return "seasonUser{id{" + user.id.ToString() + "},lapDry{" + lapDry.ToString("mm':'ss':'fff") + "},lapWet{" + lapWet.ToString("mm':'ss':'fff") + "},lapDryLink{" + lapDryLink + "},lapWetLink{" + lapWetLink + "},team{" + team.id.ToString() + "},team1{" + prefferedTeams[0].id + "},team2{" + prefferedTeams[1].id + "},team3{" + prefferedTeams[2].id + "}}";
                string val = "seasonUser{id{" + user.id.ToString() + "}";
                val += ",team{" + team.id.ToString() + "}";
                if(accessLevel > AccessLevels.user)
                {
                    val += ",lapDry{" + lapDry.ToString("mm':'ss':'fff") + "},lapWet{" + lapWet.ToString("mm':'ss':'fff") + "},lapDryLink{" + lapDryLink + "},lapWetLink{" + lapWetLink + "},team1{" + prefferedTeams[0].id + "},team2{" + prefferedTeams[1].id + "},team3{" + prefferedTeams[2].id + "}";
                }

                val += "}";

                return val;
            }

            public void FillVariables(TimeSpan lapDry, TimeSpan lapWet, string lapDryLink, string lapWetLink, Team team1, Team team2, Team team3, int priority = int.MaxValue)
            {
                this.lapDry = lapDry;
                this.lapWet = lapWet;
                this.lapDryLink = lapDryLink;
                this.lapWetLink = lapWetLink;
                this.priority = priority;
                this.prefferedTeams[0] = team1;
                this.prefferedTeams[1] = team2;
                this.prefferedTeams[2] = team3;
            }

            public bool LoadFromSql(object[] data)
            {
                try
                {
                    if (data.Length < 10)
                    {
                        Debug.LogError("[User.LoadFromSqlSeason] Not enough data to load from, found only " + data.Length + " columns");
                    }

                    user = User.GetUser((int)data[0]);
                    lapDry = (TimeSpan)data[1];
                    lapWet = (TimeSpan)data[2];
                    lapDryLink = (string)data[3];
                    lapWetLink = (string)data[4];
                    priority = (int)data[5];
                    team = Team.GetTeam((int)data[6]);
                    prefferedTeams = new Team[3];
                    prefferedTeams[0] = Team.GetTeam((int)data[7]);
                    prefferedTeams[1] = Team.GetTeam((int)data[8]);
                    prefferedTeams[2] = Team.GetTeam((int)data[9]);

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
                        new MSQL.Value("priority", priority),
                        new MSQL.Value("team", team.id),
                        new MSQL.Value("pteam1", prefferedTeams[0].id),
                        new MSQL.Value("pteam2", prefferedTeams[1].id),
                        new MSQL.Value("pteam3", prefferedTeams[2].id)
                    };
            }
        }

        public int id;
        public int racesCount;
        public List<Race> races;
        public List<SeasonUser> users = new List<SeasonUser>();
        public RegistrationData registrationData;
        public int finishedRaces;
        public bool assigned;
        public bool finished
        {
            get
            {
                return finishedRaces >= racesCount || users.Count <= 20;
            }
        }
        public string prefix;
        public Track registrationTrack;

        public bool good;
        public void Log()
        {
            Debug.Log("Season " + id.ToString(), ConsoleColor.White, false);
            if(assigned)
            {
                Debug.Log(" (assigned)", ConsoleColor.White, false);
            }
            Debug.Log(": ");
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

        /// <summary>
        /// Assiigns driver to season after registration period
        /// </summary>
        /// <param name="sUser"></param>
        public void AssignDriverAfterRegistartion(SeasonUser user)
        {
            int[] teams = new int[Team.GetLength()];
            for(int i = 0;i<users.Count;i++)
            {
                teams[users[i].team.id]++;
            }

            for(int i = 0;i<user.prefferedTeams.Length;i++)
            {
                if(teams[user.prefferedTeams[i].id] < 2)
                {
                    user.team = user.prefferedTeams[i];
                    return;
                }
            }

            for(int i = 0;i<teams.Length;i++)
            {
                if(teams[i] < 2)
                {
                    user.team = Team.GetTeam(i);
                    return;
                }
            }

            if(user.team == null)
            {
                user.team = Team.other;
            }

            //UpdateLeaderboards();
        }

        /// <summary>
        /// Assigns drivers to season
        /// </summary>
        public void AssignDrivers()
        {
            try
            {
                for (int i = 0; i < users.Count; i++)
                {
                    int index = i;
                    double min = double.MaxValue;
                    for (int j = i; j < users.Count; j++)
                    {
                        if (users[j].user.donate > users[index].user.donate)
                        {
                            index = j;
                        }
                        else if (users[j].user.donate == users[index].user.donate)
                        { 
                            double sum = users[j].lapDry.TotalMilliseconds + users[j].lapWet.TotalMilliseconds;
                            if (sum < min)
                            {
                                min = sum;
                                index = j;
                            }
                        }
                    }
                    var pom = users[i];
                    users[i] = users[index];
                    users[index] = pom;

                    users[i].priority = i;
                }
                // Null all users, so it's cleared for assigning
                for (int i = 0; i < users.Count; i++)
                {
                    users[i].team = null;
                }

                int[] teams = new int[Team.teams.Length];
                for (int i = 0; i < users.Count; i++)
                {
                    for (int j = 0; j < users[i].prefferedTeams.Length; j++)
                    {
                        if (users[i].prefferedTeams[j].id == Team.other.id)
                        {
                            break;
                        }

                        if (teams[users[i].prefferedTeams[j].id] < 2)
                        {
                            users[i].team = users[i].prefferedTeams[j];
                            teams[users[i].prefferedTeams[j].id]++;
                            break;
                        }
                    }
                }

                for (int i = 0; i < users.Count; i++)
                {
                    if (users[i].team != null) continue;
                    for (int j = 0; j < teams.Length; j++)
                    {
                        if (teams[j] < 2)
                        {
                            users[i].team = Team.GetTeam(j);
                            teams[j]++;
                            break;
                        }
                    }
                    if (users[i].team == null)
                    {
                        users[i].team = Team.other;
                    }
                }


                assigned = true;
                //UpdateLeaderboards();
                
                Debug.Log("Assigned drivers");
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[Error assigning drivers]");
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
                
                //UpdateLeaderboards();

                return true;
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[WindWingAppServer.ParseSeasonString]");
                return false;
            }
        }

        public string Serialize(bool includeUsers = false, User singleUser = null)
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

            if(includeUsers)
            {
                if(users.Count > 0)
                {
                    str += ",users{";

                    for(int i = 0;i<users.Count;i++)
                    {
                        str += users[i].Serialize(SeasonUser.AccessLevels.admin);
                        if(i != users.Count - 1)
                        {
                            str += ',';
                        }
                    }

                    str += "}";
                }
            }
            else if(singleUser != null)
            {
                if (users.Count > 0)
                {
                    str += ",users{";

                    for (int i = 0; i < users.Count; i++)
                    {
                        if (users[i].user.id == singleUser.id)
                        {
                            str += users[i].Serialize(SeasonUser.AccessLevels.owner);
                            if (i != users.Count - 1)
                            {
                                str += ',';
                            }
                        }
                    }

                    str += "}";
                }
            }


            return str + "}";
        }

        public bool LoadFromSql(object[] data)
        {
            if (data.Length < 8)
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
                assigned = (bool)data[7];

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

        static int[] points = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
        int GetPoints(int place)
        {
            place--;
            if(place < 0 || place >= points.Length)
            {
                return 0;
            }
            return points[place];
        }

        string leaderboardsStr = "";
        public void UpdateLeaderboards()
        {
            int[] points = new int[users.Count];
            for (int i = 0; i < races.Count; i++)
            {
                Race.Result bestLap = null;
                for (int j = 0; j < races[i].results.Count; j++)
                {
                    if(bestLap == null)
                    {
                        bestLap = races[i].results[j];
                    }
                    else if(races[i].results[j].bestLap < bestLap.bestLap)
                    {
                        bestLap = races[i].results[j];
                    }
                    if(races[i].results[j].dnf || !races[i].results[j].started)
                    {
                        continue;
                    }
                    // Find user
                    for (int k = 0; k < users.Count; k++)
                    {
                        if (races[i].results[j].user.id == users[k].user.id)
                        {
                            points[k] += GetPoints(races[i].results[j].place);
                            break;
                        }
                    }
                }
                if (races[i].date < DateTime.Now && bestLap != null && bestLap.bestLap != TimeSpan.Zero)
                {
                    // Must be in points to get additional best lap point
                    if (bestLap.place < 10 && bestLap.place >= 0 && !bestLap.dnf && bestLap.started)
                    {
                        // Find user
                        for (int k = 0; k < users.Count; k++)
                        {
                            if (bestLap.user.id == users[k].user.id)
                            {
                                points[k] += 1;
                                break;
                            }
                        }
                    }
                }
            }

            leaderboardsStr = points.Length.ToString() + "{";
            for (int i = 0; i < points.Length; i++)
            {
                leaderboardsStr += "(" + users[i].user.id + "," + points[i].ToString() + "," + users[i].team.shortName + ")";
                if (i != points.Length - 1)
                {
                    leaderboardsStr += ";";
                }
            }
            leaderboardsStr += "}";

        }

        public string GetLeaderboards()
        {
            UpdateLeaderboards();

            return leaderboardsStr;
        }
    }
}
