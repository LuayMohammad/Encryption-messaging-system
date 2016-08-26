using allFileEncrypt;
using ConsolClient;
using RSAExample;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form2 : Form
    {
        static TcpClient tcpClient ;
        static NetworkStream networkStream;
        static String mykey;
        static String subPath;
        static Color chatcolor = Color.DarkCyan;
        private static List<user> friends = new List<user>();
        private int my_id;
        string file;
        String all_files ;
        bool wait = true;
        Form3 file_view;
        bool no = false;
        CspParameters cspp = new CspParameters();
        RSACryptoServiceProvider rsa;
        // Public key file
        string PubKeyFile ;
        // Key container name for
        // private/public key value pair.
        string keyName;
        static List<user> users_publickey = new List<user>();
        bool isfile = false;
        static X509Certificate2 baseCert;
        public Form2(TcpClient tcp, String key, string path,List<user> t,int id)
        {
            tcpClient = tcp;
            mykey = key;
            subPath = path;
            friends = t;
            networkStream = tcpClient.GetStream();
            my_id = id;
            //Asymetric 
            keyName = mykey.Trim();

            InitializeComponent();
            fill_users();
            Thread thread = new Thread(new ThreadStart(() => chat(tcpClient, subPath)));
            thread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
                string encryptedstring;
                label6.Text = users.Rows[users.CurrentCell.RowIndex].Cells[0].Value.ToString();
                label5.Text = users.Rows[users.CurrentCell.RowIndex].Cells[1].Value.ToString();
                label5.Visible = true;
                label6.Visible = true;
                StreamWriter writer = new StreamWriter(networkStream);
                string msg = textBox1.Text;
                   //string []temp = msg.Split('-');
                encryptedstring = StringCipher.Encrypt(msg/*temp[1]*/, mykey);
                    encryptedstring = label6.Text/*temp[0]*/ + "-" + encryptedstring;
                writer.WriteLine(encryptedstring);
                writer.Flush();
                
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Green;
                richTextBox1.AppendText("ME : ");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;

                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.AppendText(msg + "\u2028");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
                //networkStream.Close();
        }

        public void chat(TcpClient tcpClient/*NetworkStream networkStream*/, string subPath)
        {
            NetworkStream networkStream1 = tcpClient.GetStream();
            while (true)
            {
                StreamReader reader = new StreamReader(networkStream1);
                string myinfo = reader.ReadLine();
                if (myinfo == "r-f")
                {
                    string file_info = reader.ReadLine();
                    string[] info = file_info.Split('-');
                    //label4.Visible = true;
                    AppendText("Client [ " + info[1] + " ] sending " , info[0]);
                }
                else if (myinfo == "hash-f")
                {
                    String signature = reader.ReadLine();
                    String hashedDocument = reader.ReadLine();
                    String sender_publickey = reader.ReadLine();

                    var verified = VerifySignature(Convert.FromBase64String(hashedDocument), Convert.FromBase64String(signature), sender_publickey);

                    if (verified)
                    {
                        file_view.Label4Text = "The digital signature for [ " + file_view.datagridview + " ]  has been correctly verified";
                        file_view.download = "true";
                    }
                    else
                    {
                        file_view.Label4Text = "The digital signature for [ " + file_view.datagridview + " ]  has NOT been correctly verified";
                        file_view.download = "false";
                    }
                }
                else if (myinfo == "hash-f1")
                {
                    IFormatter unformatter = new BinaryFormatter();
                    Stream unstream = new FileStream(@"D:\SERVER_TEST\" + subPath + "\\dec" + file_view.datagridview, FileMode.Open, FileAccess.Read, FileShare.Read);
                    MyObject unobj = (MyObject)unformatter.Deserialize(unstream);
                    unstream.Close();

                    String signature = unobj.str;
                    var sha = SHA256.Create();
                    byte[] hashedDocument = sha.ComputeHash(unobj.obj);
                    String sender_publickey = reader.ReadLine();

                    var verified = VerifySignature(hashedDocument, Convert.FromBase64String(signature), sender_publickey);

                    if (verified)
                    {
                        file_view.Label4Text = "The digital signature for [ " + file_view.datagridview + " ]  has been correctly verified";
                        cspp.KeyContainerName = keyName;
                        rsa = new RSACryptoServiceProvider(cspp);
                        rsa.PersistKeyInCsp = true;
                        byte[] k = Convert.FromBase64String((file_view.key_index).ToString());
                        Stream keystream = new MemoryStream(k);
                        DecryptFile(keystream, "D:\\SERVER_TEST\\" + subPath + "\\Sym_key.txt");
                        StreamReader sr = new StreamReader("D:\\SERVER_TEST\\" + subPath + "\\Sym_key.txt");
                        string keytxt = sr.ReadToEnd();
                        sr.Close();
                        Stream decfile = new FileStream(@"D:\SERVER_TEST\" + subPath + "\\" + file_view.datagridview, FileMode.Create, FileAccess.ReadWrite);

                        byte[] x = unobj.obj;
                        byte[] dec = Decrypt(x, keytxt);
                        file_view.LabelText = "The file " + file_view.datagridview + " has been downloaded ";
                        decfile.Write(dec, 0, dec.Length);
                        decfile.Close();
                        keystream.Close();
                    }
                    else
                    {
                        file_view.Label4Text = "The digital signature for [ " + file_view.datagridview + " ]  has NOT been correctly verified";
                        file_view.download = "false";
                    }
                }
                else if (myinfo == "d-f")
                {
                    Stream stream = new FileStream(@"D:\SERVER_TEST\" + subPath + "\\dec" + file_view.datagridview, FileMode.Create, FileAccess.ReadWrite);
                    Byte[] bytes = new Byte[1024];
                    int length = 1;
                    while (length > 0)
                    {
                        if (networkStream.DataAvailable)
                        {
                            length = networkStream.Read(bytes, 0, bytes.Length);
                            stream.Write(bytes, 0, length);
                            if (length < 1024)
                                break;
                        }
                    }
                    stream.Close();
                    if (file_view.key_method == 0)
                    {
                        Stream decfile = new FileStream(@"D:\SERVER_TEST\" + subPath + "\\" + file_view.datagridview, FileMode.Create, FileAccess.ReadWrite);
                        byte[] x = File.ReadAllBytes(@"D:\SERVER_TEST\" + subPath + "\\dec" + file_view.datagridview);
                        byte[] dec = Decrypt(x, (file_view.key_index).ToString());
                        file_view.LabelText = "The file " + file_view.datagridview + " has been downloaded ";
                        decfile.Write(dec, 0, dec.Length);
                        decfile.Close();
                    }

                    else if (file_view.key_method == 1)
                    {
                        cspp.KeyContainerName = keyName;
                        rsa = new RSACryptoServiceProvider(cspp);
                        rsa.PersistKeyInCsp = true;
                        byte[] x = File.ReadAllBytes(@"D:\SERVER_TEST\" + subPath + "\\dec" + file_view.datagridview);
                        Stream tempstream = new MemoryStream(x);
                        DecryptFile(tempstream, "D:\\SERVER_TEST\\" + subPath + "\\" + file_view.datagridview);
                        file_view.LabelText = "The file " + file_view.datagridview + " has been downloaded ";
                        tempstream.Close();
                    }

                    else if ((file_view.key_method == 2) || (file_view.key_method == 5))
                    {
                        cspp.KeyContainerName = keyName;
                        rsa = new RSACryptoServiceProvider(cspp);
                        rsa.PersistKeyInCsp = true;
                        byte[] k = Convert.FromBase64String((file_view.key_index).ToString());
                        Stream keystream = new MemoryStream(k);
                        DecryptFile(keystream,"D:\\SERVER_TEST\\" + subPath + "\\Sym_key.txt");
                        StreamReader sr = new StreamReader("D:\\SERVER_TEST\\" + subPath + "\\Sym_key.txt");
                        string keytxt = sr.ReadToEnd();
                        sr.Close();
                        Stream decfile = new FileStream(@"D:\SERVER_TEST\" + subPath + "\\" + file_view.datagridview, FileMode.Create, FileAccess.ReadWrite);
                        byte[] x = File.ReadAllBytes(@"D:\SERVER_TEST\" + subPath + "\\dec" + file_view.datagridview);
                        byte[] dec = Decrypt(x, keytxt);
                        file_view.LabelText = "The file " + file_view.datagridview + " has been downloaded ";
                        decfile.Write(dec, 0, dec.Length);
                        decfile.Close();
                        keystream.Close();
                    }
                    else if (file_view.key_method == 4)
                    {
                        cspp.KeyContainerName = keyName;
                        rsa = new RSACryptoServiceProvider(cspp);
                        rsa.PersistKeyInCsp = true;

                        byte[] x = File.ReadAllBytes(@"D:\SERVER_TEST\" + subPath + "\\dec" + file_view.datagridview);
                        byte[] ClearData = rsa.Decrypt(x, true);

                        Stream decfile = new FileStream("D:\\SERVER_TEST\\" + subPath + "\\" + file_view.datagridview, FileMode.Create, FileAccess.ReadWrite);
                        decfile.Write(ClearData, 0, ClearData.Length);
                        decfile.Close();
                        file_view.LabelText = "The file " + file_view.datagridview + " has been downloaded ";
                    }
                    else if (file_view.key_method == 6)
                    {
                        file_view.LabelText = "The file " + file_view.datagridview + " has been downloaded without being decrypt";
                    }

                    else if (file_view.key_method == 7)
                    {
                        IFormatter unformatter = new BinaryFormatter();
                        Stream unstream = new FileStream("D:\\SERVER_TEST\\" + subPath + "\\" + label4.Text + "_cert.pfx", FileMode.Open, FileAccess.Read, FileShare.Read);
                        MyObject unobj = (MyObject)unformatter.Deserialize(unstream);
                        unstream.Close();
                        X509Certificate2 certification = new X509Certificate2(unobj.obj, unobj.str);
                        rsa = (RSACryptoServiceProvider)certification.PrivateKey;
                        rsa.PersistKeyInCsp = true;

                        byte[] k = Convert.FromBase64String((file_view.key_index).ToString());
                        byte[] w = rsa.Decrypt(k, true);
                        
                        //Stream keystream = new MemoryStream(k);
                        //DecryptFile(keystream, "D:\\SERVER_TEST\\" + subPath + "\\Sym_key.txt");
                        Stream deckeyfile = new FileStream("D:\\SERVER_TEST\\" + subPath + "\\Sym_key.txt", FileMode.Create, FileAccess.ReadWrite);
                        deckeyfile.Write(w, 0, w.Length);
                        deckeyfile.Close();

                        StreamReader sr = new StreamReader("D:\\SERVER_TEST\\" + subPath + "\\Sym_key.txt");
                        string keytxt = sr.ReadToEnd();
                        sr.Close();

                        Stream decfile = new FileStream(@"D:\SERVER_TEST\" + subPath + "\\" + file_view.datagridview, FileMode.Create, FileAccess.ReadWrite);
                        byte[] x = File.ReadAllBytes(@"D:\SERVER_TEST\" + subPath + "\\dec" + file_view.datagridview);
                        byte[] dec = Decrypt(x, keytxt);
                        file_view.LabelText = "The file " + file_view.datagridview + " has been downloaded ";
                        decfile.Write(dec, 0, dec.Length);
                        decfile.Close();
                        //keystream.Close();
                    }

                }
                else
                {
                    if (myinfo != null)
                    {
                    
                        string[] info = myinfo.Split('-');
                        if (info[0] == "on")
                        {
                            int c = 0;
                            while (c < users.RowCount)
                            {
                                if (users.Rows[c].Cells[0].Value.ToString() == info[1])
                                    users.Rows[c].DefaultCellStyle.BackColor = Color.LightGreen;
                                c++;
                            }
                        }
                        else if (info[0] == "off")
                        {
                            int c = 0;
                            while (c < users.RowCount)
                            {
                                if (users.Rows[c].Cells[0].Value.ToString() == info[1])
                                    users.Rows[c].DefaultCellStyle.BackColor = Color.LightPink;
                                c++;
                            }
                        }
                        else if (info[0] == "key")
                        {
                            if (info[1]=="no") {
                                no = true;
                            }
                            else {
                                PubKeyFile = info[1];
                            }
                            wait = false;
                        }
                        else if (info[0] == "keyn")
                        {
                            if (info[1] == "no")
                            {
                                no = true;
                            }
                            else
                            {
                                for (int user_pk = 0; user_pk < users_publickey.Count; user_pk++) { 
                                    string temp = reader.ReadLine();
                                    users_publickey[user_pk].key = temp;
                                }
                            }
                            wait = false;
                        }
                        else if (info[0] == "cert")
                        {
                            MemoryStream ms = new MemoryStream();
                            Byte[] bytes = new Byte[1024];
                            int length = 1;
                            while (length > 0)
                            {
                                if (networkStream.DataAvailable)
                                {
                                    length = networkStream.Read(bytes, 0, bytes.Length);
                                    ms.Write(bytes, 0, length);
                                    if (length < 1024)
                                        break;
                                }
                            }
                            ms.Close();
                            Byte[] certData = ms.ToArray();
                            X509Certificate newCert = new X509Certificate(certData);
                            users_publickey[0].key= newCert.GetPublicKeyString();
                            baseCert = new X509Certificate2(newCert);
                            wait = false;

                            Form4 certificate_details = new Form4(newCert);
                            certificate_details.Show();
                            Application.Run();
                        }
                        else if ((info[0] != "error") && (info[0] != "no"))
                        {
                            string decryptedstring = StringCipher.Decrypt(info[1], info[2]);
                            string from = "From Client [ " + info[0] + " ] : ";
                            AppendText(from, decryptedstring);
                        }
                        else if (info[0] == "no")
                        {
                            all_files = info[1];
                            wait = false;
                        }
                        else
                        {
                            AppenderrorText(info[0], info[1]);
                        }
                    }
                }
            }
        }

        delegate void AppendTextDelegate(string text, string decryptedstring);

        private void AppendText(string text, string decryptedstring)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new AppendTextDelegate(this.AppendText), new object[] { text, decryptedstring });
            }
            else
            {

                decryptedstring = decryptedstring + "\n";
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = chatcolor;
                richTextBox1.AppendText(text);
                richTextBox1.SelectionColor = richTextBox1.ForeColor;

                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.AppendText(decryptedstring);
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
            }
        }

        private void AppenderrorText(string text, string decryptedstring)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new AppendTextDelegate(this.AppenderrorText), new object[] { text, decryptedstring });
            }
            else
            {

                decryptedstring = decryptedstring +"\n";
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.AppendText("From Server : [ " + text + " ] : \u2028");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;

                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Orange;
                richTextBox1.AppendText(decryptedstring + "\u2028");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
            }
        }
      

        private void button2_Click(object sender, EventArgs e)
        {
            var openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string filefromdialog = openFileDialog1.FileName;
                try
                {
                    label6.Text = users.Rows[users.CurrentCell.RowIndex].Cells[0].Value.ToString();
                    label5.Text = users.Rows[users.CurrentCell.RowIndex].Cells[1].Value.ToString();
                    label5.Visible = true;
                    label6.Visible = true;
                    StreamWriter writer = new StreamWriter(networkStream);
                    writer.WriteLine("f-s");
                    writer.Flush();
                    textBox1.Text = filefromdialog;
                    textBox1.Enabled = false;
                    button4.Visible = true;
                    button5.Visible = true;
                    button12.Visible = true;
                    button7.Visible = true;
                    button1.Enabled = false;
                    isfile = true;
                    file = filefromdialog;
                }
                catch (IOException)
                {
                }
            }
        }

        public static byte[] Encrypt(byte[] input,string mykey)
        {
            PasswordDeriveBytes pdb =
              new PasswordDeriveBytes(mykey, // Change this
              new byte[] { 0x43, 0x87, 0x23, 0x72 }); // Change this
            MemoryStream ms = new MemoryStream();
            Aes aes = new AesManaged();
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            CryptoStream cs = new CryptoStream(ms,
              aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(input, 0, input.Length);
            cs.Close();
            return ms.ToArray();
        }
        public static byte[] Decrypt(byte[] input,string mykey)
        {
            PasswordDeriveBytes pdb =
              new PasswordDeriveBytes(mykey, // Change this
              new byte[] { 0x43, 0x87, 0x23, 0x72 }); // Change this
            MemoryStream ms = new MemoryStream();
            Aes aes = new AesManaged();
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            CryptoStream cs = new CryptoStream(ms,
              aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(input, 0, input.Length);
            cs.Close();
            return ms.ToArray();
        }
        
        public void fill_users(){
            int c=0;
            int j = 0;
            users.RowCount = friends.Count-1;
            while(c<friends.Count){
                if (friends[c].id == my_id)
                    label4.Text = friends[c].username;

                else
                {
                    users.Rows[j].Cells[0].Value = (friends[c].id).ToString();
                    users.Rows[j].Cells[1].Value = (friends[c].username).ToString();
                    if (friends[c].logged_in)
                        users.Rows[j].DefaultCellStyle.BackColor = Color.LightGreen;
                    else
                        users.Rows[j].DefaultCellStyle.BackColor = Color.LightPink;
                    j++;
                }
                c++;
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in users.SelectedRows)
            {
                label6.Text = row.Cells[0].Value.ToString();
                label5.Text = row.Cells[1].Value.ToString();
                label5.Visible = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
                StreamWriter writer = new StreamWriter(networkStream);
                StreamReader reader = new StreamReader(networkStream);
                writer.WriteLine(my_id+"-get:my:files");
                writer.Flush();
                while (wait) { };
                wait = true;

            file_view = new Form3(tcpClient, mykey, subPath, my_id,all_files);
            file_view.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            //First Symetric way
            writer.WriteLine("0");
            writer.Flush();
            byte[] dataFromFile = File.ReadAllBytes(file);
            //size = dataToSend.Length;
            button2.Text = Path.GetFileName(file);
            byte[] dataToSend = Encrypt(dataFromFile, mykey);
            writer.WriteLine(Path.GetFileName(file));
            writer.Flush();
            isfile = false;
            string tar_s = label6.Text;

            if (users.SelectedRows.Count > 0)
            {
                for (int i = 0; i < users.SelectedRows.Count; i++)
                    if(i==0)
                        tar_s = users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
                    else
                        tar_s += "*" + users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
            }
            

            writer.WriteLine(tar_s);
            writer.Flush();

            fileStream.Write(dataToSend, 0, dataToSend.Length);
            fileStream.Flush();
            textBox1.Text = "";
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Visible = false;
            button5.Visible = false;
            button12.Visible = false;
            button7.Visible = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            StreamReader readpublickey = new StreamReader(networkStream);
            isfile = false;
            //Encrypt Method 
            writer.WriteLine("1");
            writer.Flush();

            //Target Method 
            string tar_s = label6.Text;
            writer.WriteLine(tar_s);
            writer.Flush();

            //Get Target Public Key
            while (wait) { };
            wait = true;
            if (!no)
            {
                rsa = new RSACryptoServiceProvider(cspp);
                rsa.FromXmlString(PubKeyFile);
                rsa.PersistKeyInCsp = true;

                button2.Text = Path.GetFileName(file);
                byte[] dataToSend = EncryptFile(file);
                writer.WriteLine(Path.GetFileName(file));
                writer.Flush();

                fileStream.Write(dataToSend, 0, dataToSend.Length);
                fileStream.Flush();
            }
            else {
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.AppendText("SERVER : ");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;

                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.AppendText("The User you select not have a public key yet !!  \u2028");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
            }
            no = false;
            textBox1.Text = "";
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Visible = false;
            button5.Visible = false;
            button12.Visible = false;
            button7.Visible = false;
        }

        private byte[] EncryptFile(string inFile,bool x=false)
        {

            // Create instance of Rijndael for
            // symetric encryption of the data.
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;
            ICryptoTransform transform = rjndl.CreateEncryptor();

            // Use RSACryptoServiceProvider to
            // enrypt the Rijndael key.
            // rsa is previously instantiated: 
            //    rsa = new RSACryptoServiceProvider(cspp);
            byte[] keyEncrypted = rsa.Encrypt(rjndl.Key, false);

            // Create byte arrays to contain
            // the length values of the key and IV.
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            LenK = BitConverter.GetBytes(lKey);
            int lIV = rjndl.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            // Write the following to the FileStream
            // for the encrypted file (outFs):
            // - length of the key
            // - length of the IV
            // - ecrypted key
            // - the IV
            // - the encrypted cipher content
            // Change the file's extension to ".enc"


            using (MemoryStream outFs = new MemoryStream())
            {

                outFs.Write(LenK, 0, 4);
                outFs.Write(LenIV, 0, 4);
                outFs.Write(keyEncrypted, 0, lKey);
                outFs.Write(rjndl.IV, 0, lIV);

                // Now write the cipher text using
                // a CryptoStream for encrypting.
                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {

                    // By encrypting a chunk at
                    // a time, you can save memory
                    // and accommodate large files.
                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;

                    if (x)
                    {
                        Stream inFs = GenerateStreamFromString(inFile);
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        }
                        while (count > 0);
                        inFs.Close();
                    }
                    else
                    {
                        FileStream inFs = new FileStream(inFile, FileMode.Open);
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        }
                        while (count > 0);
                        inFs.Close();
                    }
                    
                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();
                }
                outFs.Close();
                return outFs.ToArray();
            }

        }

        private void DecryptFile(Stream inFile, String outFile)
        {

            // Create instance of Rijndael for
            // symetric decryption of the data.
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;

            // Create byte arrays to get the length of
            // the encrypted key and IV.
            // These values were stored as 4 bytes each
            // at the beginning of the encrypted package.
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            // Consruct the file name for the decrypted file.

            // Use FileStream objects to read the encrypted
            // file (inFs) and save the decrypted file (outFs).
            using (Stream inFs = inFile)
            {

                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(LenK, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                // Convert the lengths to integer values.
                int lenK = BitConverter.ToInt32(LenK, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                // Determine the start postition of
                // the ciphter text (startC)
                // and its length(lenC).
                int startC = lenK + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                // Create the byte arrays for
                // the encrypted Rijndael key,
                // the IV, and the cipher text.
                byte[] KeyEncrypted = new byte[lenK];
                byte[] IV = new byte[lenIV];

                // Extract the key and IV
                // starting from index 8
                // after the length values.
                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenK);
                inFs.Seek(8 + lenK, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);
                // Use RSACryptoServiceProvider
                // to decrypt the Rijndael key.
                byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                // Decrypt the key.
                ICryptoTransform transform = rjndl.CreateDecryptor(KeyDecrypted, IV);

                // Decrypt the cipher text from
                // from the FileSteam of the encrypted
                // file (inFs) into the FileStream
                // for the decrypted file (outFs).
                if (File.Exists(outFile))
                {
                    File.Delete(outFile);
                }
                using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                {

                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];


                    // By decrypting a chunk a time,
                    // you can save memory and
                    // accommodate large files.

                    // Start at the beginning
                    // of the cipher text.
                    inFs.Seek(startC, SeekOrigin.Begin);
                    using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);

                        }
                        while (count > 0);

                        outStreamDecrypted.FlushFinalBlock();
                        outStreamDecrypted.Close();
                    }
                    outFs.Close();
                }
                inFs.Close();
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;
            String xmlpublickey = rsa.ToXmlString(false);
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter p_k = new StreamWriter(fileStream);

            p_k.WriteLine("p_k-"+xmlpublickey);
            p_k.Flush();

            button6.ForeColor = Color.Green;
            button6.Text = "DONE";
        }

        private void button7_Click(object sender, EventArgs e)
        {
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            //First Symetric way
            writer.WriteLine("2");
            writer.Flush();
            isfile = false;
            string tar_s = label6.Text;

            if (users.SelectedRows.Count > 0)
            {
                for (int i = 0; i < users.SelectedRows.Count; i++)
                {
                    if (i == 0)
                        tar_s = users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
                    else
                        tar_s += "*" + users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
                    users_publickey.Add(new user(Convert.ToInt32(users.Rows[users.SelectedRows[i].Index].Cells[0].Value), "NULL"));
                }
            }

            writer.WriteLine(tar_s);
            writer.Flush();

            String session_key = RandomString(16);

            byte[] dataFromFile = File.ReadAllBytes(file);
            //size = dataToSend.Length;
            button2.Text = Path.GetFileName(file);
            byte[] dataToSend = Encrypt(dataFromFile,/* mykey*/session_key);
            writer.WriteLine(Path.GetFileName(file));
            writer.Flush();

            
            fileStream.Write(dataToSend, 0, dataToSend.Length);
            fileStream.Flush();

            while (wait) { };
            wait = true;

            for (int u = 0; u < users_publickey.Count; u++) {

                if (users_publickey[u].key != "NULL")
                {
                    PubKeyFile = users_publickey[u].key;
                    rsa = new RSACryptoServiceProvider(cspp);
                    rsa.FromXmlString(PubKeyFile);
                    rsa.PersistKeyInCsp = true;

                    byte[] keyToSend = EncryptFile(/*mykey*/session_key, true);
                    string cipherText = Convert.ToBase64String(keyToSend);
                    writer.WriteLine(cipherText);
                    writer.Flush();
                }
                else
                {
                    writer.WriteLine("NULL");
                    writer.Flush();
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Red;
                    richTextBox1.AppendText("SERVER : ");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;

                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Black;
                    richTextBox1.AppendText("The User ["+users_publickey[u].id+"] you select not have a public key yet !!  \u2028");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }
            }
            users_publickey.Clear();
                textBox1.Text = "";
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Visible = false;
            button5.Visible = false;
            button12.Visible = false;
            button7.Visible = false;
        }

        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void button8_Click(object sender, EventArgs e)
        {
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            StreamReader readpublickey = new StreamReader(networkStream);

            if (!isfile)
            {
                //Noteficate Server 
                writer.WriteLine("f-s");
                writer.Flush();
            }
            //Encrypt Method 
            writer.WriteLine("1");
            writer.Flush();

            //Target Method 
            string tar_s = users.Rows[users.CurrentCell.RowIndex].Cells[0].Value.ToString();
            writer.WriteLine(tar_s);
            writer.Flush();

            //Get Target Public Key
            while (wait) { };
            wait = true;
            if (!no)
            {
                rsa = new RSACryptoServiceProvider(cspp);
                rsa.FromXmlString(PubKeyFile);
                rsa.PersistKeyInCsp = true;
                byte[] dataToSend;
                if (!isfile)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textBox1.Text);
                    dataToSend = rsa.Encrypt(data, true);
                    writer.WriteLine("loc%" + textBox2.Text + ".txt");
                    writer.Flush();
                }
                else
                {
                    byte[] data = File.ReadAllBytes(file);
                    dataToSend = rsa.Encrypt(data, true);
                    writer.WriteLine("loc%" + Path.GetFileName(file));
                    writer.Flush();
                }
                fileStream.Write(dataToSend, 0, dataToSend.Length);
                fileStream.Flush();
            }
            else
            {
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.AppendText("SERVER : ");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;

                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.AppendText("The User you select not have a public key yet !!  \u2028");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
            }
            textBox1.Text = "";
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Visible = false;
            button5.Visible = false;
            button12.Visible = false;
            button7.Visible = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            //First Symetric way
            writer.WriteLine("5");
            writer.Flush();
            isfile = false;
            string tar_s = label6.Text;

            if (users.SelectedRows.Count > 0)
            {
                for (int i = 0; i < users.SelectedRows.Count; i++)
                {
                    if (i == 0)
                        tar_s = users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
                    else
                        tar_s += "*" + users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
                    users_publickey.Add(new user(Convert.ToInt32(users.Rows[users.SelectedRows[i].Index].Cells[0].Value), "NULL"));
                }
            }

            writer.WriteLine(tar_s);
            writer.Flush();

            String session_key = RandomString(16);

            byte[] dataFromFile = File.ReadAllBytes(file);
            //size = dataToSend.Length;
            button2.Text = Path.GetFileName(file);
            byte[] dataToSend = Encrypt(dataFromFile,/* mykey*/session_key);
            writer.WriteLine(Path.GetFileName(file));
            writer.Flush();
            fileStream.Write(dataToSend, 0, dataToSend.Length);
            fileStream.Flush();



            /******* IMPLEMENT HASHING ALGORITHM **********/

            byte[] hashedDocument;

            using (var sha256 = SHA256.Create())
            {
                hashedDocument = sha256.ComputeHash(dataFromFile);
            }

            var byte_signature = SignData(hashedDocument);
            String signature = Convert.ToBase64String(byte_signature);

            writer.WriteLine(signature);
            writer.Flush();

            String hasing = Convert.ToBase64String(hashedDocument);
            writer.WriteLine(hasing);
            writer.Flush();
            /*******    END HASHING ALGORITHM    **********/


            while (wait) { };
            wait = true;

            for (int u = 0; u < users_publickey.Count; u++)
            {

                if (users_publickey[u].key != "NULL")
                {
                    PubKeyFile = users_publickey[u].key;
                    rsa = new RSACryptoServiceProvider(cspp);
                    rsa.FromXmlString(PubKeyFile);
                    rsa.PersistKeyInCsp = true;

                    byte[] keyToSend = EncryptFile(/*mykey*/session_key, true);
                    string cipherText = Convert.ToBase64String(keyToSend);
                    writer.WriteLine(cipherText);
                    writer.Flush();
                }
                else
                {
                    writer.WriteLine("NULL");
                    writer.Flush();
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Red;
                    richTextBox1.AppendText("SERVER : ");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;

                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Black;
                    richTextBox1.AppendText("The User [" + users_publickey[u].id + "] you select not have a public key yet !!  \u2028");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }
            }
            users_publickey.Clear();
            textBox1.Text = "";
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Visible = false;
            button5.Visible = false;
            button12.Visible = false;
            button7.Visible = false;
        }

        public byte[] SignData(byte[] hashOfDataToSign)
        {
            cspp.KeyContainerName = keyName;
            using (rsa = new RSACryptoServiceProvider(cspp))
            {
                rsa.PersistKeyInCsp = true;
                var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                rsaFormatter.SetHashAlgorithm("SHA256");

                return rsaFormatter.CreateSignature(hashOfDataToSign);
            }
        }

        public bool VerifySignature(byte[] hashOfDataToSign, byte[] signature, String PublicKeyFile)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                
                //rsa.ImportParameters(PubKeyFile);
                rsa.FromXmlString(PublicKeyFile);
                rsa.PersistKeyInCsp = false;
                var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm("SHA256");
                return rsaDeformatter.VerifySignature(hashOfDataToSign, signature);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            //First Symetric way
            writer.WriteLine("6");
            writer.Flush();
            isfile = false;
            string tar_s = label6.Text;

            if (users.SelectedRows.Count > 0)
            {
                for (int i = 0; i < users.SelectedRows.Count; i++)
                {
                    if (i == 0)
                        tar_s = users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
                    else
                        tar_s += "*" + users.Rows[users.SelectedRows[i].Index].Cells[0].Value.ToString();
                    users_publickey.Add(new user(Convert.ToInt32(users.Rows[users.SelectedRows[i].Index].Cells[0].Value), "NULL"));
                }
            }

            writer.WriteLine(tar_s);
            writer.Flush();

            String session_key = RandomString(16);

            byte[] dataFromFile = File.ReadAllBytes(file);
            //size = dataToSend.Length;
            button2.Text = Path.GetFileName(file);
            byte[] dataToSend = Encrypt(dataFromFile,/* mykey*/session_key);
            writer.WriteLine(Path.GetFileName(file));
            writer.Flush();

            /******* IMPLEMENT HASHING ALGORITHM **********/
            MyObject file_sign = new MyObject();
            byte[] hashedDocument;

            using (var sha256 = SHA256.Create())
            {
                hashedDocument = sha256.ComputeHash(dataToSend);
            }

            var byte_signature = SignData(hashedDocument);
            String signature = Convert.ToBase64String(byte_signature);
            file_sign.str = signature;
            file_sign.obj = dataToSend;

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("MyFile.bin", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, file_sign);
            stream.Close();

            byte[] obj_tosend = File.ReadAllBytes("MyFile.bin");
            fileStream.Write(obj_tosend, 0, obj_tosend.Length);
            fileStream.Flush();

            /*******    END HASHING ALGORITHM    **********/


            while (wait) { };
            wait = true;

            for (int u = 0; u < users_publickey.Count; u++)
            {

                if (users_publickey[u].key != "NULL")
                {
                    PubKeyFile = users_publickey[u].key;
                    rsa = new RSACryptoServiceProvider(cspp);
                    rsa.FromXmlString(PubKeyFile);
                    rsa.PersistKeyInCsp = true;

                    byte[] keyToSend = EncryptFile(/*mykey*/session_key, true);
                    string cipherText = Convert.ToBase64String(keyToSend);
                    writer.WriteLine(cipherText);
                    writer.Flush();
                }
                else
                {
                    writer.WriteLine("NULL");
                    writer.Flush();
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Red;
                    richTextBox1.AppendText("SERVER : ");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;

                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Black;
                    richTextBox1.AppendText("The User [" + users_publickey[u].id + "] you select not have a public key yet !!  \u2028");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }
            }
            users_publickey.Clear();
            textBox1.Text = "";
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Visible = false;
            button5.Visible = false;
            button12.Visible = false;
            button7.Visible = false;
        }

        private void button11_Click(object sender, EventArgs e)
        {

            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);

            byte[] pfx = Certificate.CreateSelfSignCertificatePfx("O=SystemSecurityHomework,CN="+label4.Text+",SN=client", DateTime.Now, DateTime.Now.AddYears(1),textBox3.Text);
            
            MyObject pfx_certificate = new MyObject();
            pfx_certificate.str=textBox3.Text;
            pfx_certificate.obj=pfx;
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("D:\\SERVER_TEST\\" + subPath+"\\"+label4.Text+"_cert.pfx", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, pfx_certificate);
            stream.Close();

            X509Certificate certif = new X509Certificate(pfx, textBox3.Text);
            byte[] certData = certif.Export(X509ContentType.Cert);

            writer.WriteLine(":clientcertificate:-" + label4.Text + "_cert.pfx");
            writer.Flush();

            fileStream.Write(certData, 0, certData.Length);
            fileStream.Flush();

            button11.Text = "Done :D";
            /*
            X509Certificate2 certification = new X509Certificate2(pfx, textBox3.Text);
            byte[] publicBytes = certification.RawData;

            RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)certification.PrivateKey;
            byte[] signedData = rsa.SignData(new System.Text.UTF8Encoding().GetBytes("Test"), new SHA1CryptoServiceProvider());

            RSACryptoServiceProvider rsa2 = (RSACryptoServiceProvider)new X509Certificate2(publicBytes).PublicKey.Key;

            bool verified = rsa2.VerifyData(new System.Text.UTF8Encoding().GetBytes("Test"), new SHA1CryptoServiceProvider(), signedData);
            */

        }

        private void button12_Click(object sender, EventArgs e)
        {
            NetworkStream fileStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            writer.WriteLine("7");
            writer.Flush();
            users_publickey.Clear();
            isfile = false;
            string tar_s = label6.Text;
            users_publickey.Add(new user(Convert.ToInt32(users.Rows[users.SelectedRows[0].Index].Cells[0].Value), "NULL"));
            writer.WriteLine(tar_s);
            writer.Flush();

            String session_key = RandomString(16);

            byte[] dataFromFile = File.ReadAllBytes(file);
            button2.Text = Path.GetFileName(file);
            byte[] dataToSend = Encrypt(dataFromFile,session_key);
            writer.WriteLine(Path.GetFileName(file));
            writer.Flush();

            fileStream.Write(dataToSend, 0, dataToSend.Length);
            fileStream.Flush();

            while (wait) { };
            wait = true;

                if (users_publickey[0].key != "NULL")
                {
                    PubKeyFile = users_publickey[0].key;
                    rsa = (RSACryptoServiceProvider)new X509Certificate2(baseCert).PublicKey.Key;
                    //rsa.FromXmlString("<RSAKeyValue><Modulus>" + PubKeyFile + "</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
                    rsa.PersistKeyInCsp = true;

                    byte[] keyToSend = rsa.Encrypt(Encoding.UTF8.GetBytes(session_key), true);//EncryptFile(/*mykey*/session_key, true); //rsa.Encrypt(Encoding.UTF8.GetBytes(session_key), true);
                    string cipherText = Convert.ToBase64String(keyToSend);
                    writer.WriteLine(cipherText);
                    writer.Flush();
                }
                else
                {
                    writer.WriteLine("NULL");
                    writer.Flush();
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Red;
                    richTextBox1.AppendText("SERVER : ");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;

                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Black;
                    richTextBox1.AppendText("The User [" + users_publickey[0].id + "] you select not have a public key yet !!  \u2028");
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }
            users_publickey.Clear();
            textBox1.Text = "";
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Visible = false;
            button5.Visible = false;
            button12.Visible = false;
            button7.Visible = false;

        }

    }
}
