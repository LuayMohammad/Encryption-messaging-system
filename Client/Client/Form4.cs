using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form4 : Form
    {
        public Form4(X509Certificate newCert)
        {
            InitializeComponent();
            String[] details = (newCert.Issuer).Split(',');
            textBox6.Text = details[0];
            textBox5.Text = details[1];
            textBox2.Text = details[2];
            textBox3.Text = newCert.GetEffectiveDateString();
            textBox4.Text = newCert.GetExpirationDateString();
            textBox1.Text = newCert.GetPublicKeyString();
        }

    }
}
