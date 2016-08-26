using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ConsolServer;
using allFileEncrypt;
using System.Data;
using System.Data.SqlClient;


namespace ConsoleServer
{
    class Program
    {
        //private TcpListener listener;
        private static List<TcpClient> conn_client = new List<TcpClient>();
        private static List<Thread> conn_Threads = new List<Thread>();
        private static List<String> conn_keys = new List<String>();
        private static List<Tuple<user, Boolean>> conn_auth = new List<Tuple<user, Boolean>>();
         
        static SqlConnection conn = new SqlConnection();
        private static void Main(string[] args)
        {

            TcpListener listener = new TcpListener(IPAddress.Any, 6576);
            listener.Start();
            Console.WriteLine("Listenerstart...");
            int indeces = 0;
            conn_auth.Add(new Tuple<user, Boolean>(new user("luay", "123"),false));
            conn_auth.Add(new Tuple<user, Boolean>(new user("Amer", "456"),false));
            conn_auth.Add(new Tuple<user, Boolean>(new user("Molham", "789"),false));
            conn_auth.Add(new Tuple<user, Boolean>(new user("soso", "741"),false));
            while (indeces<10)
            {
                /*var*/
                TcpClient client = listener.AcceptTcpClient();
                conn_client.Add(client);
                conn_keys.Add("NULL");
                conn_Threads.Add(run(client, indeces));
                conn_Threads[indeces].Start();
                indeces++;
            }
            Console.WriteLine("Stop Listening...");
            Console.ReadLine();
            listener.Stop();
        }

        private static Thread run(TcpClient client, int index)
        {
            Thread thread = new Thread(new ThreadStart(() => ChatListen(client, index)));
            return thread;
        }

        private static void ChatListen(TcpClient tcpClient, int index)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            StreamReader reader = new StreamReader(networkStream);
            StreamWriter writer = new StreamWriter(networkStream);

            authentication :
            string x = reader.ReadLine();
            if (x == "e")
            { 
                reader.Close();
                writer.Close();
                networkStream.Close();
            }
            string[] auth= x.Split(':');
            Console.WriteLine("Intialaizing Client : " + index);
            
            int user=0;
            bool accpeted=false;
            int id = 0;
                while (user<conn_auth.Count())
                {
                    if((conn_auth[user].Item1.get_name()==auth[0]) && (conn_auth[user].Item1.get_pass()==auth[1])){
                        if (conn_auth[user].Item2)
                        {
                            Console.WriteLine("dublicated session: " + auth[0] + " : " + auth[1] + " from client [ " + index + " ] already started by another client");
                            writer.WriteLine("-2");
                            writer.Flush();
                            goto authentication;
                        }
                        else
                        {
                            accpeted = true ;
                            break;
                        }
                    }
                    user++;
                }

            if (!accpeted) {
                Console.WriteLine("Faild login with : " + auth[0] + " : " + auth[1] + " from client [ "+index+" ]");
                writer.WriteLine("-1");
                writer.Flush();
                goto authentication;
            }

            writer.WriteLine(index);
            writer.Flush();
            string key = RandomString(16);
            conn_keys.Insert(index, key);
            writer.WriteLine(key);
            writer.Flush();

            Console.WriteLine("index [" + index + "] has key [ " + conn_keys[index] + " ]");
            while(true){
                StreamReader msg = new StreamReader(networkStream);
                string msg_type = msg.ReadLine();

                if (msg_type == "f-s")
                {
                    NetworkStream nS = tcpClient.GetStream();
                    string filename = reader.ReadLine();
                    int receiver = Convert.ToInt32(reader.ReadLine());
                    Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + index);

                    Stream stream = new FileStream(@"D:\SERVER_TEST\" + filename, FileMode.Create, FileAccess.ReadWrite);

                    Byte[] bytes = new Byte[1024];
                    int length = 1;
                    while (length > 0)
                    {
                        if (nS.DataAvailable)
                        {
                            length = nS.Read(bytes, 0, bytes.Length);
                            stream.Write(bytes, 0, length);
                            if (length < 1024)
                                break;
                        }
                    }
                    stream.Close();
                    Console.WriteLine("Client [ " + index + " ] : finishing upload file ; file is on server ...");
                    Console.WriteLine("sending notification to client [ " + receiver + " ] : about file from client [ " + index + "]");
                    NetworkStream target = conn_client[receiver].GetStream();
                    StreamWriter recv = new StreamWriter(target);
                    recv.WriteLine("r-f");
                    recv.Flush();
                    recv.WriteLine(filename + "-" + index);
                    recv.Flush();

                    /*
                    Console.WriteLine("sending configration to client [ " + receiver + " ] : to recive from client [ " + index + "]");

                    recv.WriteLine(filename + "-" + index + "-" + StringCipher.Encrypt(conn_keys[index], conn_keys[receiver]));
                    recv.Flush();

                    byte[] dataToSend = File.ReadAllBytes(@"D:\SERVER_TEST\" + filename);
                    target.Write(dataToSend, 0, dataToSend.Length);
                    target.Flush();
                    */
                }
                else
                {
                    if (msg_type != null)
                    {
                        string[] info = msg_type.Split('-');
                        if (info.Count() == 2)
                        {
                            if (info[1] == "x")
                                break;
                            Console.WriteLine("Client [ " + index + " ] sending " + info[1] + " to " + info[0]);
                            if ((-1 < Convert.ToInt32(info[0])) && (Convert.ToInt32(info[0]) < conn_client.Count()))
                            {
                                NetworkStream target = conn_client[Convert.ToInt32(info[0])].GetStream();
                                StreamWriter recv = new StreamWriter(target);
                                recv.WriteLine(index + "-" + info[1] + "-" + conn_keys[index]);
                                recv.Flush();
                            }
                            else
                            {
                                Console.WriteLine("Invaliad Request from client [ " + index + "]");
                                writer.WriteLine("error- the client [ " + Convert.ToInt32(info[0]) + "] isn't available");
                                writer.Flush();
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Client [ " + index + " ] left the session");
            networkStream.Close();
            tcpClient.Close();
        }

        public static bool chk_value(string p){

            int i = 0;
            while (i < p.Length)
                if ((int)p[i] > 127)
                    return false;
            return true;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
