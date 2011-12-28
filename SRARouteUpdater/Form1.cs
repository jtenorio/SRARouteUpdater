using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//my using
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SRARouteUpdater
{
    public partial class Form1 : Form
    {
        protected string rutaSra;
        protected string[] nuevasRutas;


        public Form1()
        {
            InitializeComponent();
            this.rutaSra = "";
            this.nuevasRutas = new string[500];
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            openFileDialog1.Filter = "Ejecutable del SRA 2|*.exe";
            openFileDialog1.FileName = "SRA2FULL";
            openFileDialog1.Title = "Ubique el ejecutable del SRA 2 en su computador";
            openFileDialog1.ShowDialog();
            string ruta = openFileDialog1.FileName.ToString();
            string[] partesDeRuta = ruta.Split('\\');

            foreach (string pruta in partesDeRuta)
            {
                if (pruta != "SRA2FULL.exe")
                {
                    this.rutaSra += pruta+"\\";
                }
            }

            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            string[] rutasActuales = Directory.GetFiles(this.rutaSra, "*.txi");
            string[] rutasActuales2 = Directory.GetFiles(this.rutaSra, "*.tvi");

            foreach (string file in rutasActuales)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            //progressBar1.Value = 25;

            foreach (string file2 in rutasActuales2)
            {
                File.SetAttributes(file2, FileAttributes.Normal);
                File.Delete(file2);
            }
            //progressBar1.Value = 50;

            //copiar nuevas rutas

            //obtener el listado de las nuevas rutas

            string url = "http://www.tamevirtual.org/sra_rutas/";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            int i = 0;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex(GetDirectoryListingRegexForUrl(url));
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0)
                    {
                        
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                this.nuevasRutas[i] =(match.Groups["name"].ToString());
                                i++;
                            }
                        }
                    }
                }
            }

            //colocar el valor maximo de la barra
            progressBar1.Maximum = i;

            WebClient client = new WebClient();
            byte[] data;

            for (int j = 0; j < i; j++)
            {

                if (this.nuevasRutas[j].Trim() != "Parent Directory")
                {
                    //MessageBox.Show(this.nuevasRutas[j]);
                    data = client.DownloadData("http://www.tamevirtual.org/sra_rutas/" + this.nuevasRutas[j].Trim());
                    File.WriteAllBytes(this.rutaSra + this.nuevasRutas[j].Trim(), data);
                    File.SetAttributes(this.rutaSra + this.nuevasRutas[j].Trim(), FileAttributes.Hidden);
                    
                    //File.SetAttributes(this.rutaSra + this.nuevasRutas[j].Trim(), FileAttributes.ReadOnly);
                    //File.SetAttributes(this.rutaSra + this.nuevasRutas[j].Trim(), FileAttributes.System);
                    progressBar1.Value = j;
                }
            }

            MessageBox.Show("Route update completed!", "Route Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Exit();

        }

        public static string GetDirectoryListingRegexForUrl(string url)
        {
            if (url.Equals("http://www.tamevirtual.org/sra_rutas/"))
            {
                return "<a href=\".*\">(?<name>.*)</a>";
            }
            throw new NotSupportedException();
        }


        
    }
}