using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using ConsolClient;

namespace Client
{
    public partial class Form3 : Form
    {
        static TcpClient tcpClient;
        static String mykey;
        static String subPath;
        int my_id;
        static List<String> keys=new List<string>();
        static List<int> method = new List<int>();
        public Form3(TcpClient tcp, String key, string path, int id, String all_files)
        {
            tcpClient = tcp;
            mykey = key;
            subPath = path;
            my_id = id;
            InitializeComponent();
            if (all_files != "no")
            {
                fill_files(all_files);
            }
            else
            {
                button1.Enabled = false;
                button2.Enabled = false;
            }
        }

        public void fill_files(String files)
        {
            String[] file = files.Split('%');
            int c = 0;
            keys.Clear();
            method.Clear();
            dataGridView1.RowCount = file.Count();
            while (c < file.Count())
            {
                String[] one_file = file[c].Split('!');
                    dataGridView1.Rows[c].Cells[0].Value = one_file[1];
                    dataGridView1.Rows[c].Cells[1].Value = one_file[2];
                    dataGridView1.Rows[c].Cells[2].Value = one_file[3];
                    dataGridView1.Rows[c].Cells[3].Value = one_file[0];
                    keys.Add(one_file[4]);
                    method.Add(Convert.ToInt32(one_file[5]));
                    if (File.Exists(one_file[2] +"\\"+subPath+"\\"+ one_file[1]))
                    {
                        dataGridView1.Rows[c].DefaultCellStyle.BackColor = Color.LightGreen;
                    }
                c++;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            writer.WriteLine(":finf-"+dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString());
            writer.Flush();
            button2.Enabled = true;
            button3.Enabled = true;
            //Decrypt(@"D:\SERVER_TEST\" + subPath + "\\" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString(), @"D:\SERVER_TEST\" + subPath + "\\" + "Dec" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString(), StringCipher.Decrypt(keys[dataGridView1.CurrentCell.RowIndex], mykey));
        }


        public string LabelText
        {
            get
            {
                return this.label3.Text;
            }
            set
            {
                this.label3.Text = value;
                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
            }
        }

        public string Label4Text
        {
            get
            {
                return this.label4.Text;
            }
            set
            {
                this.label4.Text = value;
                this.label3.Text = "File is ready to Download";
                this.button1.Enabled = true;
                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].DefaultCellStyle.BackColor = Color.AliceBlue;
            }
        }

        public string datagridview
        {
            get
            {
                return this.dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString();
            }
        }

        public string key_index
        {
            get
            {
                int index = this.dataGridView1.CurrentCell.RowIndex;
                return keys[index].Trim();
            }
        }

        public int key_method
        {
            get
            {
                int index = this.dataGridView1.CurrentCell.RowIndex;
                return method[index];
            }
        }

        public String download
        {
            set
            {
                this.button1.Enabled = Convert.ToBoolean(value);
                if (!Convert.ToBoolean(value)) {
                    this.label3.Text = "The File isn't secured";
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].DefaultCellStyle.BackColor = Color.OrangeRed;
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            int index = this.dataGridView1.CurrentCell.RowIndex;
            if ((method[index] == 5) || (method[index] == 7))
            {
                writer.WriteLine(":verifyHash-" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString() + "-" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value.ToString());
                writer.Flush();
            }
            else if (method[index] == 6)
            {
                writer.WriteLine(":verifyHash1-" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString() + "-" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value.ToString());
                writer.Flush();
            }
            else
                this.label3.Text = "The File hasn't signature";
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (method[dataGridView1.CurrentCell.RowIndex] == 5)
            { 
                button2.Enabled = true;
                button1.Enabled = false;
                label4.Text = " ";
                label3.Text = " ";
                label4.Visible = true;
                button3.Visible = true;
                textBox1.Visible = true;
            }
            else if (method[dataGridView1.CurrentCell.RowIndex] == 6) {
                button2.Enabled = false;
                button3.Enabled = false;
                button1.Enabled = true;
                label4.Text = " ";
                label3.Text = " ";
                label4.Visible = true;
                button3.Visible = true;
                textBox1.Visible = true;
            }
            else
            {
                button2.Enabled = false;
                button1.Enabled = true;
                label4.Text = " ";
                label3.Text = " ";
                label4.Visible = false;
                button3.Visible = false;
                textBox1.Text = "";
                textBox1.Visible = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            int index = this.dataGridView1.CurrentCell.RowIndex;
            if ((method[index] == 5) || (method[index] == 7))
            {
                writer.WriteLine(":verifyHash-" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString() + "-" + textBox1.Text);
                writer.Flush();
            }
            else if (method[index] == 6)
            {
                writer.WriteLine(":verifyHash1-" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString() + "-" + textBox1.Text);
                writer.Flush();
            }
            else
                this.label3.Text = "The File hasn't signature";
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

    }
}
