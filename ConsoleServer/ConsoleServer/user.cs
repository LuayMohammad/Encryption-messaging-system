using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace allFileEncrypt
{
    public class user
    {
        public String username { get; set; }
        public String Password { get; set; }
        public String key { get; set; }
        public TcpClient client { get; set;}
        public int id { get; set; }
        public bool logged_in { get; set; }
        public user(string name, string pass,int id,String key)
        {
            this.username = name;
            this.Password = pass;
            this.id = id;
            this.logged_in = false;
            this.key = key;
        }
    }
}
