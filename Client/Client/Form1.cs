using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using allFileEncrypt;


namespace Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        static List<user> u = new List<user>();
        private void button3_Click(object sender, EventArgs e)
        {
            TcpClient tcpClient = new TcpClient("localhost", 6576);
            Boolean init_log = true;
            if (init_log)
            {
                NetworkStream networkStream = tcpClient.GetStream();
                StreamWriter ipwriter = new StreamWriter(networkStream);
                StreamReader reader = new StreamReader(networkStream);


                String msg = textBox1.Text + ":" + textBox2.Text;
                ipwriter.WriteLine(msg);
                ipwriter.Flush();

                string myinfo = reader.ReadLine();
                if (myinfo == "-1")
                {
                    label2.Text = "Error : username or password not vaild ";
                }

                else if (myinfo == "-2")
                {
                    label2.Text = "Error : username has already started a session ";
                }

                else
                {
                    textBox1.Visible = false;
                    textBox2.Visible = false;
                    button3.Visible = false;
                    label3.Visible = true;
                    int my_id = Convert.ToInt32(reader.ReadLine());
                    label3.Text = "My id is [ " + my_id + " ]";

                    String mykey = reader.ReadLine();
                    label2.Text = "My Key is recieved ...";

                    string subPath = textBox1.Text; // your code goes here
                    bool exists = System.IO.Directory.Exists(@"D:\SERVER_TEST\" + subPath);
                    if (!exists)
                        System.IO.Directory.CreateDirectory(@"D:\SERVER_TEST\" + subPath);
                    convert_tolist(myinfo);
                    Form2 user_view = new Form2(tcpClient, mykey, subPath, u, my_id);
                    user_view.Show();
                    this.Hide();
                    init_log = false;
                }
            }
        }

        public static void convert_tolist(String temp)
        {
            String []all_users=temp.Split('-');
            int c = 0;
            while (c < all_users.Count())
            {
                String []one_user=all_users[c].Split(':');
                u.Add(new user(one_user[1], Convert.ToInt32(one_user[0]),Convert.ToBoolean(one_user[2])));
                c++;
            }
        }
        void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
           /* NetworkStream networkStream = tcpClient.GetStream();
            // Prompt user to save his data
            if(e.CloseReason == CloseReason.UserClosing)
            {
                StreamWriter ipwriter = new StreamWriter(networkStream);
                String msg = "e";
                ipwriter.WriteLine(msg);
                ipwriter.Flush();
                tcpClient.Close();
            }

            // Autosave and clear up ressources
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                StreamWriter ipwriter = new StreamWriter(networkStream);
                String msg = "e";
                ipwriter.WriteLine(msg);
                ipwriter.Flush();
                tcpClient.Close();
            }
            */ 
        }
    }
}
