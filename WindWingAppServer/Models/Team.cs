﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WindWingAppServer.Models
{
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

        public static List<List<string>> TeamsToSQL()
        {
            List<List<string>> data = new List<List<string>>();
            for (int i = 0; i < teams.Length; i++)
            {
                data.Add(teams[i].ToSQL());
            }
            return data;
        }

        public static Team[] teams = new Team[] {
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
    }
}
