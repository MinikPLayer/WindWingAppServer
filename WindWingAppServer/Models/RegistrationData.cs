using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindWingAppServer.Models
{
    public class RegistrationData
    {
        public bool opened
        {
            get
            {
                return endDate > DateTime.Now;
            }
        }
        public DateTime endDate;

        public RegistrationData(bool opened, DateTime endTime)
        {
            this.endDate = endTime;
        }

        bool ParseSinglePacket(string header, string content)
        {
            try
            {
                switch (header)
                {
                    case "date":
                        endDate = DateTime.Parse(content, new CultureInfo("de-DE"));
                        return true;

                    default:
                        Debug.LogError("[RegistrationData.ParseSinglePacket] Unknown header");
                        return false;
                }
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[RegistrationData.ParseSinglePacket]");
                return false;
            }
        }

        public bool Deserialize(string str)
        {
            try
            {
                if (!str.StartsWith("registration{"))
                {
                    Debug.LogError("[RegistrationData.Deserialize] It's no a race packet, bad magic");
                    return false;
                }

                str = str.Substring(0, str.Length - 1).Remove(0, 13); // remove race{ and }

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
                                Debug.LogError("[RegistrationData.Deserialize] Cannot parse packet with header: " + packets[i].Substring(0, j));
                                return false;
                            }
                            done = true;
                            break;
                        }
                    }
                    if (!done)
                    {
                        Debug.LogError("[RegistrationData.Deserialize] Cannot find a closing bracket, incomplete packet");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.Exception(e, "[RegistrationData.Deserialize]");
                return false;
            }
        }

        public string Serialize()
        {
            return "registration{date{" + endDate.ToString(new CultureInfo("de-DE")) + "}}";
        }
    }
}
