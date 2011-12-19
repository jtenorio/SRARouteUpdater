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

namespace SRARouteUpdater
{
    public partial class Form1 : Form
    {
        protected string rutaSra;
        public Form1()
        {
            InitializeComponent();
            this.rutaSra = "";
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
            progressBar1.Value = 25;

            foreach (string file2 in rutasActuales2)
            {
                File.SetAttributes(file2, FileAttributes.Normal);
                File.Delete(file2);
            }
            progressBar1.Value = 50;

            //copiar nuevas rutas

            string[] rutasNuevas = Directory.GetFiles("http://www.tamevirtual.org/sra_rutas", "*.txi");
        }

        
    }
}