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
        private static List<Thread> conn_Threads = new List<Thread>();
        private static List<user> conn_auth = new List<user>();
         
        static SqlConnection conn = new SqlConnection();
        private static void Main(string[] args)
        {
            
            bool exists = System.IO.Directory.Exists(@"D:\SERVER_TEST\certificate");
            if (!exists)
                System.IO.Directory.CreateDirectory(@"D:\SERVER_TEST\certificate");

            bool pexists = System.IO.Directory.Exists(@"D:\SERVER_TEST\publickeys");
            if (!pexists)
                System.IO.Directory.CreateDirectory(@"D:\SERVER_TEST\publickeys");


            string sqlCon = @"Data Source=(LocalDB)\v11.0;AttachDbFilename='C:\Users\Luay AL Assadi\Documents\Visual Studio 2013\Projects\ConsoleServer\ConsoleServer\ss_server.mdf';Integrated Security=True";
            conn.ConnectionString = sqlCon;
            conn.Open();
            Console.WriteLine("Server Connected Successfully to DB");
            TcpListener listener = new TcpListener(IPAddress.Any, 6576);
            listener.Start();
            Console.WriteLine("Listenerstart...");
            int indeces = 0;

            /**** intialaizing users to dynamic array ****/ 
            SqlCommand command = new SqlCommand("SELECT * FROM users", conn);
            using (SqlDataReader result = command.ExecuteReader())
            {
                while (result.Read())
                {
                    int id = Convert.ToInt32(result[0]);
                    conn_auth.Add(new user(result[1].ToString().Trim(), result[2].ToString().Trim(), id, result[3].ToString().Trim()));
                }
            }
            
            while (indeces<15)
            {
                /*var*/
                TcpClient client = listener.AcceptTcpClient();
                //conn_client.Add(client);
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
                    if((conn_auth[user].username==auth[0]) && (conn_auth[user].Password==auth[1])){
                        if (conn_auth[user].logged_in)
                        {
                            Console.WriteLine("dublicated session: " + auth[0] + " : " + auth[1] + " from client [ " + index + " ] already started by another client");
                            writer.WriteLine("-2");
                            writer.Flush();
                            goto authentication;
                        }
                        else
                        {
                            accpeted = true ;
                            conn_auth[user].client = tcpClient;
                            conn_auth[user].logged_in = true;
                            id = user;
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
            String all_user=convert_toString(conn_auth);
            writer.WriteLine(all_user);
            writer.Flush();

            writer.WriteLine(conn_auth[id].id);
            writer.Flush();

            string key = conn_auth[id].key;
            writer.WriteLine(key);
            writer.Flush();

            Console.WriteLine("[" + id + "] : " + conn_auth[id].username + " has key [ " + conn_auth[id].key + " ]");
            int note = 0;
            while (note < conn_auth.Count)
            {
                if ((conn_auth[note].logged_in)&&(note!=id))
                {
                    NetworkStream target = conn_auth[note].client.GetStream();
                    StreamWriter recv = new StreamWriter(target);
                    recv.WriteLine("on-" + conn_auth[id].id);
                    recv.Flush();
                }
                note++;
            }
            while(true){
                StreamReader msg = new StreamReader(networkStream);
                string msg_type = msg.ReadLine();

                if (msg_type == "f-s")
                {
                    NetworkStream nS = tcpClient.GetStream();
                    string encryptMethod = reader.ReadLine();
                    if (encryptMethod == "5")
                    {
                        string a_r = reader.ReadLine();
                        String[] x_rec = a_r.Split('*');
                        /* Getting the Public Key from n user */
                        writer.WriteLine("keyn-yess");
                        writer.Flush();
                        int modified = 0;
                        string filename="";
                        filename = reader.ReadLine();
                        Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + conn_auth[id].username);

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

                        String signature = reader.ReadLine();
                        System.IO.File.WriteAllText(@"D:\SERVER_TEST\signature_" + filename + "_.txt", signature);

                        String hashfile = reader.ReadLine();
                        System.IO.File.WriteAllText(@"D:\SERVER_TEST\hash_" + filename + "_.txt", hashfile);

                        Console.WriteLine("Client [ " + conn_auth[id].username + " ] : finishing upload file ; file is on server ...");
                        for (int i = 0; i < x_rec.Length; i++)
                        {
                            int receiver = index_at(Convert.ToInt32(x_rec[i]));
                            /* ***  Add the file previliges into database *** */
                            if (File.Exists(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt"))
                            {
                                StreamReader sr = new StreamReader(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt");
                                string keytxt = sr.ReadToEnd();
                                sr.Close();
                                writer.WriteLine(keytxt);
                                writer.Flush();
                            }
                            else {
                                writer.WriteLine("NULL");
                                writer.Flush();
                            }
                        }

                        for (int i = 0; i < x_rec.Length; i++)
                        {
                            int receiver = index_at(Convert.ToInt32(x_rec[i]));

                            String Enckey = reader.ReadLine();
                            if (Enckey != "NULL")
                            {
                                SqlCommand insertCommand = new SqlCommand("INSERT INTO [file] (owner_id, file_name, file_path , enc_key ,Date_created,[method]) output INSERTED.Id VALUES (@0, @1, @2, @3 ,@4,@5);SELECT @@IDENTITY AS Ident", conn);
                                insertCommand.Parameters.Add(new SqlParameter("0", conn_auth[id].id));
                                insertCommand.Parameters.Add(new SqlParameter("1", filename));
                                insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\"));
                                insertCommand.Parameters.Add(new SqlParameter("3", Enckey));
                                insertCommand.Parameters.Add(new SqlParameter("4", DateTime.Now));
                                insertCommand.Parameters.Add(new SqlParameter("5", 5));
                                modified = Convert.ToInt32(insertCommand.ExecuteScalar());


                                SqlCommand insertfile = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2);SELECT @@IDENTITY AS Ident", conn);
                                insertfile.Parameters.Add(new SqlParameter("0", modified));
                                insertfile.Parameters.Add(new SqlParameter("1", conn_auth[receiver].id));
                                insertfile.Parameters.Add(new SqlParameter("2", 1));
                                insertfile.ExecuteNonQuery();

                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : about file from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine(filename + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                            else {
                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : publickey request from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine("Need your public key " + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                        }
                    }
                    else if (encryptMethod == "7") {
                        string a_r = reader.ReadLine();
                        /* Getting the Public Key from n user */
                        writer.WriteLine("cert-yes");
                        writer.Flush();
                        int modified = 0;
                        string filename = "";
                        filename = reader.ReadLine();
                        Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + conn_auth[id].username);

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

                        Console.WriteLine("Client [ " + conn_auth[id].username + " ] : finishing upload file ; file is on server ...");
                        int receiver = index_at(Convert.ToInt32(a_r));
                            /* ***  Add the file previliges into database *** */
                        if (File.Exists(@"D:\SERVER_TEST\certificate\" + conn_auth[receiver].username + "_cert.pfx"))
                            {
                                Byte[] cert = File.ReadAllBytes(@"D:\SERVER_TEST\certificate\" + conn_auth[receiver].username + "_cert.pfx");
                                nS.Write(cert, 0, cert.Length);
                                nS.Flush();
                            }
                            else
                            {
                                writer.WriteLine("NULL");
                                writer.Flush();
                            }

                            String Enckey = reader.ReadLine();
                            if (Enckey != "NULL")
                            {
                                SqlCommand insertCommand = new SqlCommand("INSERT INTO [file] (owner_id, file_name, file_path , enc_key ,Date_created,[method]) output INSERTED.Id VALUES (@0, @1, @2, @3 ,@4,@5);SELECT @@IDENTITY AS Ident", conn);
                                insertCommand.Parameters.Add(new SqlParameter("0", conn_auth[id].id));
                                insertCommand.Parameters.Add(new SqlParameter("1", filename));
                                insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\"));
                                insertCommand.Parameters.Add(new SqlParameter("3", Enckey));
                                insertCommand.Parameters.Add(new SqlParameter("4", DateTime.Now));
                                insertCommand.Parameters.Add(new SqlParameter("5", 7));
                                modified = Convert.ToInt32(insertCommand.ExecuteScalar());


                                SqlCommand insertfile = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2);SELECT @@IDENTITY AS Ident", conn);
                                insertfile.Parameters.Add(new SqlParameter("0", modified));
                                insertfile.Parameters.Add(new SqlParameter("1", conn_auth[receiver].id));
                                insertfile.Parameters.Add(new SqlParameter("2", 1));
                                insertfile.ExecuteNonQuery();

                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : about file from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine(filename + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                            else
                            {
                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : publickey request from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine("Need your public key " + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                    }
                    else if (encryptMethod == "6")
                    {
                        string a_r = reader.ReadLine();
                        String[] x_rec = a_r.Split('*');
                        /* Getting the Public Key from n user */
                        writer.WriteLine("keyn-yess");
                        writer.Flush();
                        int modified = 0;
                        string filename = "";
                        filename = reader.ReadLine();
                        Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + conn_auth[id].username);

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

                        Console.WriteLine("Client [ " + conn_auth[id].username + " ] : finishing upload file ; file is on server ...");
                        for (int i = 0; i < x_rec.Length; i++)
                        {
                            int receiver = index_at(Convert.ToInt32(x_rec[i]));
                            /* ***  Add the file previliges into database *** */
                            if (File.Exists(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt"))
                            {
                                StreamReader sr = new StreamReader(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt");
                                string keytxt = sr.ReadToEnd();
                                sr.Close();
                                writer.WriteLine(keytxt);
                                writer.Flush();
                            }
                            else
                            {
                                writer.WriteLine("NULL");
                                writer.Flush();
                            }
                        }

                        for (int i = 0; i < x_rec.Length; i++)
                        {
                            int receiver = index_at(Convert.ToInt32(x_rec[i]));

                            String Enckey = reader.ReadLine();
                            if (Enckey != "NULL")
                            {
                                SqlCommand insertCommand = new SqlCommand("INSERT INTO [file] (owner_id, file_name, file_path , enc_key ,Date_created,[method]) output INSERTED.Id VALUES (@0, @1, @2, @3 ,@4,@5);SELECT @@IDENTITY AS Ident", conn);
                                insertCommand.Parameters.Add(new SqlParameter("0", conn_auth[id].id));
                                insertCommand.Parameters.Add(new SqlParameter("1", filename));
                                insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\"));
                                insertCommand.Parameters.Add(new SqlParameter("3", Enckey));
                                insertCommand.Parameters.Add(new SqlParameter("4", DateTime.Now));
                                insertCommand.Parameters.Add(new SqlParameter("5", 6));
                                modified = Convert.ToInt32(insertCommand.ExecuteScalar());


                                SqlCommand insertfile = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2);SELECT @@IDENTITY AS Ident", conn);
                                insertfile.Parameters.Add(new SqlParameter("0", modified));
                                insertfile.Parameters.Add(new SqlParameter("1", conn_auth[receiver].id));
                                insertfile.Parameters.Add(new SqlParameter("2", 1));
                                insertfile.ExecuteNonQuery();

                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : about file from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine(filename + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                            else
                            {
                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : publickey request from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine("Need your public key " + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                        }
                    }
                    else if (encryptMethod == "1")
                    {
                        string target = reader.ReadLine();
                        int receiver = index_at(Convert.ToInt32(target));
                        StreamWriter publickey = new StreamWriter(networkStream);
                        if (File.Exists(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt"))
                        {
                            StreamReader sr = new StreamReader(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt");
                            string keytxt = sr.ReadToEnd();
                            sr.Close();
                            publickey.WriteLine("key-" + keytxt);
                            publickey.Flush();

                            string filename = reader.ReadLine();
                            String[] f_name = filename.Split('%');
                            if (f_name.Length == 1)
                            {
                                Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + conn_auth[id].username);

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

                                /* ***  Add the file info into database *** */
                                SqlCommand insertCommand = new SqlCommand("INSERT INTO [file] (owner_id, file_name, file_path , enc_key ,Date_created,[method]) output INSERTED.Id VALUES (@0, @1, @2, @3 ,@4,@5);SELECT @@IDENTITY AS Ident", conn);
                                insertCommand.Parameters.Add(new SqlParameter("0", conn_auth[id].id));
                                insertCommand.Parameters.Add(new SqlParameter("1", filename));
                                insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\"));
                                insertCommand.Parameters.Add(new SqlParameter("3", conn_auth[id].key));
                                insertCommand.Parameters.Add(new SqlParameter("4", DateTime.Now));
                                insertCommand.Parameters.Add(new SqlParameter("5", 1));
                                //insertCommand.ExecuteNonQuery();
                                int modified = Convert.ToInt32(insertCommand.ExecuteScalar());

                                /* ***  Add the file previliges into database *** */
                                SqlCommand insertfile = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2);SELECT @@IDENTITY AS Ident", conn);
                                insertfile.Parameters.Add(new SqlParameter("0", modified));
                                insertfile.Parameters.Add(new SqlParameter("1", conn_auth[receiver].id));
                                insertfile.Parameters.Add(new SqlParameter("2", 1));
                                insertfile.ExecuteNonQuery();

                                Console.WriteLine("Client [ " + conn_auth[id].username + " ] : finishing upload file ; file is on server ...");
                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : about file from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target_ns = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target_ns);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine(filename + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                            else
                            {
                                Console.WriteLine(" server is getting ready to recive file [ " + f_name[1] + " ] From Client " + conn_auth[id].username);

                                Stream stream = new FileStream(@"D:\SERVER_TEST\" + f_name[1], FileMode.Create, FileAccess.ReadWrite);

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

                                /* ***  Add the file info into database *** */
                                SqlCommand insertCommand = new SqlCommand("INSERT INTO [file] (owner_id, file_name, file_path , enc_key ,Date_created,[method]) output INSERTED.Id VALUES (@0, @1, @2, @3 ,@4,@5);SELECT @@IDENTITY AS Ident", conn);
                                insertCommand.Parameters.Add(new SqlParameter("0", conn_auth[id].id));
                                insertCommand.Parameters.Add(new SqlParameter("1", f_name[1]));
                                insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\"));
                                insertCommand.Parameters.Add(new SqlParameter("3", conn_auth[id].key));
                                insertCommand.Parameters.Add(new SqlParameter("4", DateTime.Now));
                                insertCommand.Parameters.Add(new SqlParameter("5", 4));
                                //insertCommand.ExecuteNonQuery();
                                int modified = Convert.ToInt32(insertCommand.ExecuteScalar());

                                /* ***  Add the file previliges into database *** */
                                SqlCommand insertfile = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2);SELECT @@IDENTITY AS Ident", conn);
                                insertfile.Parameters.Add(new SqlParameter("0", modified));
                                insertfile.Parameters.Add(new SqlParameter("1", conn_auth[receiver].id));
                                insertfile.Parameters.Add(new SqlParameter("2", 1));
                                insertfile.ExecuteNonQuery();

                                Console.WriteLine("Client [ " + conn_auth[id].username + " ] : finishing upload file ; file is on server ...");
                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : about file from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target_ns = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target_ns);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine(filename + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                        }
                        else
                        {
                            publickey.WriteLine("key-no");
                            publickey.Flush();

                            if (conn_auth[receiver].logged_in)
                            {
                                Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : publickey request from client [ " + conn_auth[id].username + "]");
                                NetworkStream target_ns = conn_auth[receiver].client.GetStream();
                                StreamWriter recv = new StreamWriter(target_ns);
                                recv.WriteLine("r-f");
                                recv.Flush();
                                recv.WriteLine("Need your public key " + "-" + conn_auth[id].username);
                                recv.Flush();
                            }
                        }
                    }
                    else if (encryptMethod == "2")
                    {
                        string a_r = reader.ReadLine();
                        String[] x_rec = a_r.Split('*');
                        /* Getting the Public Key from n user */
                        writer.WriteLine("keyn-yess");
                        writer.Flush();
                        int modified = 0;
                        string filename = "";
                        filename = reader.ReadLine();
                        Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + conn_auth[id].username);

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

                        Console.WriteLine("Client [ " + conn_auth[id].username + " ] : finishing upload file ; file is on server ...");
                        for (int i = 0; i < x_rec.Length; i++)
                        {
                            int receiver = index_at(Convert.ToInt32(x_rec[i]));
                            /* ***  Add the file previliges into database *** */
                            if (File.Exists(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt"))
                            {
                                StreamReader sr = new StreamReader(@"D:\SERVER_TEST\publickeys\" + conn_auth[receiver].username + ".txt");
                                string keytxt = sr.ReadToEnd();
                                sr.Close();
                                writer.WriteLine(keytxt);
                                writer.Flush();
                            }
                            else
                            {
                                writer.WriteLine("NULL");
                                writer.Flush();
                            }
                        }

                        for (int i = 0; i < x_rec.Length; i++)
                        {
                            int receiver = index_at(Convert.ToInt32(x_rec[i]));

                            String Enckey = reader.ReadLine();
                            if (Enckey != "NULL")
                            {
                                SqlCommand insertCommand = new SqlCommand("INSERT INTO [file] (owner_id, file_name, file_path , enc_key ,Date_created,[method]) output INSERTED.Id VALUES (@0, @1, @2, @3 ,@4,@5);SELECT @@IDENTITY AS Ident", conn);
                                insertCommand.Parameters.Add(new SqlParameter("0", conn_auth[id].id));
                                insertCommand.Parameters.Add(new SqlParameter("1", filename));
                                insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\"));
                                insertCommand.Parameters.Add(new SqlParameter("3", Enckey));
                                insertCommand.Parameters.Add(new SqlParameter("4", DateTime.Now));
                                insertCommand.Parameters.Add(new SqlParameter("5", 2));
                                modified = Convert.ToInt32(insertCommand.ExecuteScalar());


                                SqlCommand insertfile = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2);SELECT @@IDENTITY AS Ident", conn);
                                insertfile.Parameters.Add(new SqlParameter("0", modified));
                                insertfile.Parameters.Add(new SqlParameter("1", conn_auth[receiver].id));
                                insertfile.Parameters.Add(new SqlParameter("2", 1));
                                insertfile.ExecuteNonQuery();

                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : about file from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine(filename + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                            else
                            {
                                if (conn_auth[receiver].logged_in)
                                {
                                    Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : publickey request from client [ " + conn_auth[id].username + "]");
                                    NetworkStream target = conn_auth[receiver].client.GetStream();
                                    StreamWriter recv = new StreamWriter(target);
                                    recv.WriteLine("r-f");
                                    recv.Flush();
                                    recv.WriteLine("Need your public key " + "-" + conn_auth[id].username);
                                    recv.Flush();
                                }
                            }
                        }
                    }
                    else if (encryptMethod == "0")
                    {
                        string filename = reader.ReadLine();
                        string a_r = reader.ReadLine();
                        Console.WriteLine(" server is getting ready to recive file [ " + filename + " ] From Client " + conn_auth[id].username);

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
                        String[] x_rec = a_r.Split('*');

                        /* ***  Add the file info into database *** */
                        SqlCommand insertCommand = new SqlCommand("INSERT INTO [file] (owner_id, file_name, file_path , enc_key ,Date_created) output INSERTED.Id VALUES (@0, @1, @2, @3 ,@4);SELECT @@IDENTITY AS Ident", conn);
                        insertCommand.Parameters.Add(new SqlParameter("0", conn_auth[id].id));
                        insertCommand.Parameters.Add(new SqlParameter("1", filename));
                        insertCommand.Parameters.Add(new SqlParameter("2", "D:\\SERVER_TEST\\"));
                        insertCommand.Parameters.Add(new SqlParameter("3", conn_auth[id].key));
                        insertCommand.Parameters.Add(new SqlParameter("4", DateTime.Now));
                        //insertCommand.ExecuteNonQuery();
                        int modified = Convert.ToInt32(insertCommand.ExecuteScalar());

                        /* Sharing This file with n user */
                        for (int i = 0; i < x_rec.Length; i++)
                        {
                            int receiver = index_at(Convert.ToInt32(x_rec[i]));
                            /* ***  Add the file previliges into database *** */
                            SqlCommand insertfile = new SqlCommand("INSERT INTO file_previliges (file_id, user_id, previlage) VALUES (@0, @1, @2);SELECT @@IDENTITY AS Ident", conn);
                            insertfile.Parameters.Add(new SqlParameter("0", modified));
                            insertfile.Parameters.Add(new SqlParameter("1", conn_auth[receiver].id));
                            insertfile.Parameters.Add(new SqlParameter("2", 1));
                            insertfile.ExecuteNonQuery();


                            Console.WriteLine("Client [ " + conn_auth[id].username + " ] : finishing upload file ; file is on server ...");
                            if (conn_auth[receiver].logged_in)
                            {
                                Console.WriteLine("sending notification to client [ " + conn_auth[receiver].username + " ] : about file from client [ " + conn_auth[id].username + "]");
                                NetworkStream target = conn_auth[receiver].client.GetStream();
                                StreamWriter recv = new StreamWriter(target);
                                recv.WriteLine("r-f");
                                recv.Flush();
                                recv.WriteLine(filename + "-" + conn_auth[id].username);
                                recv.Flush();
                            }
                        }
                    }
                }
                else
                {
                    if (msg_type != null)
                    {
                        string[] info = msg_type.Split('-');
                        if (info[0] == ":clientcertificate:")
                        {
                            NetworkStream nS = tcpClient.GetStream();
                            Stream stream = new FileStream(@"D:\SERVER_TEST\certificate\" + info[1], FileMode.Create, FileAccess.ReadWrite);

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

                        }
                        else if ((info.Count() > 2) && (info[0] == ":verifyHash"))
                            {
                                Console.WriteLine("client [ " + conn_auth[id].username + " ] Asking for signature file " + info[0]);
                                Console.WriteLine("sending signature file to client [ " + conn_auth[id].username + " ] ");

                                writer.WriteLine("hash-f");
                                writer.Flush();
                                /************    Sending signature **********/
                                StreamReader sign_sr = new StreamReader("D:\\SERVER_TEST\\signature_" + info[1].Trim() + "_.txt");
                                string sign_txt = sign_sr.ReadToEnd();
                                sign_sr.Close();

                                writer.WriteLine(sign_txt);
                                writer.Flush();

                                /************    Sending hash **********/
                                StreamReader hash_sr = new StreamReader("D:\\SERVER_TEST\\hash_" + info[1].Trim() + "_.txt");
                                string hash_txt = hash_sr.ReadToEnd();
                                hash_sr.Close();

                                writer.WriteLine(hash_txt);
                                writer.Flush();

                                /************    Sending Owner PublicKey  **********/
                                StreamReader sr = new StreamReader(@"D:\SERVER_TEST\publickeys\" + info[2].Trim() + ".txt");
                                string keytxt = sr.ReadToEnd();
                                sr.Close();

                                writer.WriteLine(keytxt);
                                writer.Flush();
                            }
                        else if ((info.Count() > 2) && (info[0] == ":verifyHash1"))
                        {
                            writer.WriteLine("hash-f1");
                            writer.Flush();

                            StreamReader sr = new StreamReader(@"D:\SERVER_TEST\publickeys\" + info[2].Trim() + ".txt");
                            string keytxt = sr.ReadToEnd();
                            sr.Close();

                            writer.WriteLine(keytxt);
                            writer.Flush();
                        }
                        else if (info.Count() == 2)
                        {
                            if (info[1] == "x")
                                break;
                            if (info[0] == "p_k")
                            {   
                                StreamWriter sw = new StreamWriter(@"D:\SERVER_TEST\publickeys\" + conn_auth[id].username + ".txt", false);
                                sw.Write(info[1]);
                                sw.Close();
                            }
                            else if (info[0] == ":finf")
                            {
                                Console.WriteLine("client [ " + conn_auth[id].username + " ] Asking for file " + info[0]);
                                Console.WriteLine("sending configration to client [ " + conn_auth[id].username + " ] ");

                                byte[] dataToSend = File.ReadAllBytes("D:\\SERVER_TEST\\"+ info[1].Trim());

                                writer.WriteLine("d-f");
                                writer.Flush();

                                networkStream.Write(dataToSend, 0, dataToSend.Length);
                                networkStream.Flush();
                            }

                            else
                            {
                                int reciever = index_at(Convert.ToInt32(info[0]));
                                if (info[1] == "get:my:files")
                                {
                                    SqlCommand command = new SqlCommand("SELECT * FROM [file] f inner join file_previliges on f.id=file_previliges.file_id where user_id=@0", conn);
                                    command.Parameters.Add(new SqlParameter("0", Convert.ToInt32(info[0])));
                                    using (SqlDataReader result = command.ExecuteReader())
                                    {
                                        String user_files = "no-no";
                                        bool first = true;
                                        while (result.Read())
                                        {
                                            if (first)
                                            {
                                                user_files = "no-" + conn_auth[index_at(Convert.ToInt32(result["owner_id"]))].username + "!" + result["file_name"].ToString().Trim()
                                                     + "!" + result["file_path"].ToString().Trim() + "!" + result["Date_created"].ToString().Trim()
                                                     + "!" + result["enc_key"].ToString().Trim() + "!" + result["method"].ToString().Trim();
                                                first = false;
                                            }
                                            else
                                            {
                                                user_files += "%" + conn_auth[index_at(Convert.ToInt32(result["owner_id"]))].username + "!" + result["file_name"].ToString().Trim()
                                                         + "!" + result["file_path"].ToString().Trim() + "!" + result["Date_created"].ToString().Trim()
                                                         + "!" + result["enc_key"].ToString().Trim() + "!" + result["method"].ToString().Trim();
                                            }

                                        }
                                        //byte[] files = GetBytes(user_files);
                                        writer.WriteLine(user_files);
                                        writer.Flush();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Client [ " + conn_auth[id].username + " ] sending " + info[1] + " to " + conn_auth[reciever].username);
                                    if (conn_auth[reciever].logged_in)
                                    {
                                        NetworkStream target = conn_auth[reciever].client.GetStream();
                                        StreamWriter recv = new StreamWriter(target);
                                        recv.WriteLine(conn_auth[id].username + "-" + info[1] + "-" + conn_auth[id].key);
                                        recv.Flush();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invaliad Request from client [ " + conn_auth[id].username + "]");
                                        writer.WriteLine("error- the client [ " + reciever + "] isn't available");
                                        writer.Flush();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Client [ " + conn_auth[id].username + " ] left the session");
            note = 0;
            while (note < conn_auth.Count)
            {
                if ((conn_auth[note].logged_in) && (note != id))
                {
                    NetworkStream target = conn_auth[note].client.GetStream();
                    StreamWriter recv = new StreamWriter(target);
                    recv.WriteLine("off-" + conn_auth[id].id);
                    recv.Flush();
                }
                note++;
            }
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

        public static String convert_toString(List<user> temp){
            int c=0;
            String ans = "NULL";
            while(c<temp.Count){
                if (c == 0)
                    ans = temp[c].id + ":" + temp[c].username + ":" + temp[c].logged_in;
                else
                    ans += "-" + temp[c].id + ":" + temp[c].username + ":" + temp[c].logged_in;
                 c++;
            }
            return ans;
        }

        public static int index_at(int i)
        {
            int c = 0;
            while (c < conn_auth.Count)
            {
                if (conn_auth[c].id == i)
                    return c;
                c++;
            }
            return -1;
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
