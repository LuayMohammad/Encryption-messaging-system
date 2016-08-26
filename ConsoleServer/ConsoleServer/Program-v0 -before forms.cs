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
        private static List<Tuple<String, String>> conn_auth = new List<Tuple<String, String>>();
        static SqlConnection conn = new SqlConnection();
        private static void Main(string[] args)
        {

            /* Connect to LocalDB */
            
            string sqlCon = @"Data Source=(LocalDB)\v11.0;AttachDbFilename='C:\Users\Luay AL Assadi\Documents\Visual Studio 2013\Projects\ConsoleServer\ConsoleServer\ss_server.mdf';Integrated Security=True";
            conn.ConnectionString = sqlCon;
            conn.Open();
            Console.WriteLine("Server Connected Successfully to DB");


            TcpListener listener = new TcpListener(IPAddress.Any, 6576);
            listener.Start();
            Console.WriteLine("Listenerstart...");
            int indeces = 0;
            /*conn_auth.Add(new Tuple<user, Boolean>(new user("luay", "123"),false));
            conn_auth.Add(new Tuple<user, Boolean>(new user("Amer", "456"),false));
            conn_auth.Add(new Tuple<user, Boolean>(new user("Molham", "789"),false));
            conn_auth.Add(new Tuple<user, Boolean>(new user("soso", "741"),false));*/
            while (indeces<10)
            {
                /*var*/
                TcpClient client = listener.AcceptTcpClient();
                conn_client.Add(client);
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
           SqlCommand command = new SqlCommand("SELECT * FROM users where username=@0 and password=@1", conn);
           command.Parameters.Add(new SqlParameter("0", auth[0]));
           command.Parameters.Add(new SqlParameter("1", auth[1]));
            using (SqlDataReader result = command.ExecuteReader())
            {
                while (result.Read())
                {
                    accpeted = true;
                    conn_auth.Add(new Tuple<String,String>(auth[0],result[3].ToString()));
                    id = Convert.ToInt32(result[0]);
                    break;
                }
            }

            if (!accpeted) {
                Console.WriteLine("Faild login with : " + auth[0] + " : " + auth[1] + " from client [ "+index+" ]");
                writer.WriteLine("-1");
                writer.Flush();
                goto authentication;
            }

            command = new SqlCommand("SELECT * FROM session where user_id=@0 and sess_open=@1", conn);
            command.Parameters.Add(new SqlParameter("0", id));
            command.Parameters.Add(new SqlParameter("1", 1));
            using (SqlDataReader sess = command.ExecuteReader())
            {
                if (sess.Read() && Convert.ToInt32(sess[0]) > 0)
                {
                    Console.WriteLine("dublicated session: " + auth[0] + " : " + auth[1] + " from client [ " + index + " ] already started by another client");
                    writer.WriteLine("-2");
                    writer.Flush();
                    goto authentication;
                }
            }

            SqlCommand insertCommand = new SqlCommand("INSERT INTO session (client_id, user_id, ip , logtime ,sess_open) VALUES (@0, @1, @2, @3 ,@4)", conn);
            insertCommand.Parameters.Add(new SqlParameter("0", index));
            insertCommand.Parameters.Add(new SqlParameter("1", id));
            insertCommand.Parameters.Add(new SqlParameter("2", "127.0.0.1"));
            insertCommand.Parameters.Add(new SqlParameter("3", DateTime.Now));
            insertCommand.Parameters.Add(new SqlParameter("4", true));
            insertCommand.ExecuteNonQuery();
            writer.WriteLine(index);
            writer.Flush();
            string key = conn_auth[index].Item2;
            writer.WriteLine(key);
            writer.Flush();

            Console.WriteLine("user [" + id + " : " + conn_auth[index].Item1 + "] has key [ " + conn_auth[index].Item2 + " ]");
            while(true){
                StreamReader msg = new StreamReader(networkStream);
                string msg_type = msg.ReadLine();

                if (msg_type == "f-s")
                {
                    NetworkStream nS = tcpClient.GetStream();
                    string filename = reader.ReadLine();
                    
                    string receivers = reader.ReadLine();
                    Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + id);
                    insertCommand = new SqlCommand("INSERT INTO file (owner_id, file_name, file_path , enc_key ,Date_created) OUTPUT INSERTED.ID VALUES (@0, @1, @2, @3 ,@4)", conn);
                    insertCommand.Parameters.Add(new SqlParameter("0", id));
                    insertCommand.Parameters.Add(new SqlParameter("1", filename));
                    insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\ServerFiles\\"));
                    insertCommand.Parameters.Add(new SqlParameter("4", conn_auth[index].Item2));
                    insertCommand.Parameters.Add(new SqlParameter("3", DateTime.Now));
                    Int32 newId = (Int32)insertCommand.ExecuteNonQuery();

                    string[] recv_pre = receivers.Split('-');
                    int target_id=0;
                    while (target_id < recv_pre.Count())
                    {
                        int receiver = Convert.ToInt32(recv_pre[target_id]);
                        insertCommand = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2)", conn);
                        insertCommand.Parameters.Add(new SqlParameter("0", newId));
                        insertCommand.Parameters.Add(new SqlParameter("1", id));
                        insertCommand.Parameters.Add(new SqlParameter("2", "r-d"));
                        insertCommand.ExecuteNonQuery();

                        Stream stream = new FileStream(@"D:\SERVER_TEST\ServerFiles\" + filename, FileMode.Create, FileAccess.ReadWrite);

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
                        Console.WriteLine("Client [ " + index + " ] : finishing upload file .");
                        target_id++;

                        NetworkStream target = conn_client[receiver].GetStream();
                        StreamWriter recv = new StreamWriter(target);
                        recv.WriteLine(index + "- sending you a file to download it press d [" + filename + "] -" + conn_auth[index].Item2);
                        recv.Flush();
                        /*
                        NetworkStream target = conn_client[receiver].GetStream();
                        StreamWriter recv = new StreamWriter(target);
                        Console.WriteLine("sending configration to client [ " + receiver + " ] : to recive from client [ " + index + "]");
                        recv.WriteLine("r-f");
                        recv.Flush();

                        recv.WriteLine(filename + "-" + index + "-" + StringCipher.Encrypt(conn_auth[index].Item2, conn_auth[receiver].Item2));
                        recv.Flush();

                        byte[] dataToSend = File.ReadAllBytes(@"D:\SERVER_TEST\" + filename);
                        target.Write(dataToSend, 0, dataToSend.Length);
                        target.Flush();
                         */ 
                    }

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
                                recv.WriteLine(index + "-" + info[1] + "-" + conn_auth[index].Item2);
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

        private static void Listen(int receiver,int index,string file_name)
        {
            NetworkStream target = conn_client[receiver].GetStream();
            StreamWriter recv = new StreamWriter(target);
            Console.WriteLine("sending configration to client [ " + receiver + " ] : to recive from client [ "+index+"]");

            recv.WriteLine("r-f");
            recv.Flush();

            recv.WriteLine(file_name+"-"+index);
            recv.Flush();

            byte[] dataToSend = File.ReadAllBytes(@"D:\SERVER_TEST\" + file_name);
            target.Write(dataToSend, 0, dataToSend.Length);
            target.Flush();
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
