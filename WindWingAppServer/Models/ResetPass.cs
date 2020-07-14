using System;
using System.Collections.Generic;
using System.Text;

namespace WindWingAppServer.Models
{
    public class ResetPass
    {
        public User user;
        public string token;

        public ResetPass()
        {

        }

        public string GenerateToken()
        {
            string ret = "";
            Random rand = new Random();
            for(int i = 0;i<8;i++)
            {
                ret += (char)rand.Next((int)'A', (int)'Z');
            }

            return ret;
        }

        public ResetPass(User user)
        {
            this.user = user;
            this.token = GenerateToken();
        }

        public ResetPass(User user, string token)
        {
            this.user = user;
            this.token = token;
        }
    }
}
