using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ST.Library.UI.Demo_Image
{
    public partial class FrmImage : Form
    {
        public FrmImage() {
            InitializeComponent();
            button1.Text = "Save";
            button2.Text = "Open";
        }

        private void FrmImage_Load(object sender, EventArgs e) {
            stNodeEditor1.Dock = DockStyle.Fill;
            stNodeEditor1.TypeColor.Add(typeof(Image), Color.BlueViolet);

            STNode node = new STNodeImageInput();
            stNodeEditor1.Nodes.Add(node);

            node = new STNodeImageChannel();
            stNodeEditor1.Nodes.Add(node);


            node = new STNodeImageChannel();
            stNodeEditor1.Nodes.Add(node);
            node = new STNodeImageChannel();
            stNodeEditor1.Nodes.Add(node);
            node = new STNodeImageChannel();
            stNodeEditor1.Nodes.Add(node);

            stNodeEditor1.LoadAssembly(Application.ExecutablePath);
            stNodeEditor1.LoadAssembly(Directory.GetFiles("./", "*.dll"));
        }

        private void button1_Click(object sender, EventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "*.stn|*.stn";
            if (sfd.ShowDialog() != DialogResult.OK) return;
            stNodeEditor1.SaveCanvas(sfd.FileName);
        }

        private void button2_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.stn|*.stn";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            stNodeEditor1.Nodes.Clear();
            stNodeEditor1.LoadCanvas(ofd.FileName);
        }
    }
}
