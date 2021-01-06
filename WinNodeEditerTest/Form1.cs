using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ST.Library.UI;
using System.IO;

namespace ST.Library.UI
{
    public partial class Form1 : Form
    {
        public Form1() {
            InitializeComponent();
            button1.Text = "Lock";
            button2.Text = "Save";
            button3.Text = "Open";
        }

        private void Form1_Load(object sender, EventArgs e) {
            stNodeEditor1.Dock = DockStyle.Fill;
            stNodeEditor1.TypeColor.Add(typeof(string), Color.Yellow);
            stNodeEditor1.TypeColor.Add(typeof(Image), Color.Red);

            stNodeEditor1.SelectedChanged += new EventHandler(stNodeEditor1_SelectedChanged);
            stNodeEditor1.OptionConnected += new STNodeEditorOptionEventHandler(stNodeEditor1_OptionConnected);
            stNodeEditor1.CanvasScaled += new EventHandler(stNodeEditor1_CanvasScaled);
            for (int i = 0; i < 4; i++) {
                STNode node = new DemoNode();
                //if (i == 2)
                //    node.Mark = "这里是标记信息\r\n\t新的一行数据";
                //else
                //    node.Mark = "this is mark Info\r\nthis is new line " + i;
                stNodeEditor1.Nodes.Add(node);
            }
            stNodeEditor1.Nodes.Add(new STNodeHub());
            stNodeEditor1.Nodes.Add(new STNodeHub());

            stNodeEditor1.Nodes.Add(new NodeNumberAdd());

            stNodeEditor1.LoadAssembly(Application.ExecutablePath);
            stNodeEditor1.LoadAssembly(Directory.GetFiles("./", "*.dll"));
        }

        void stNodeEditor1_CanvasScaled(object sender, EventArgs e) {
            stNodeEditor1.ShowAlert(stNodeEditor1.CanvasScale.ToString("F2"), Color.White, Color.Black);
        }

        void stNodeEditor1_OptionConnected(object sender, STNodeEditorOptionEventArgs e) {
            Console.WriteLine(e.CurrentOption.Text + " - " + e.TargetOption.Text + " - " + e.Status);
        }

        void stNodeEditor1_SelectedChanged(object sender, EventArgs e) {
            foreach (var v in stNodeEditor1.GetSelectedNode()) {
                Console.WriteLine("Selected - " + v.Title);
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            stNodeEditor1.Nodes[0].LockOption = !stNodeEditor1.Nodes[0].LockOption;
            stNodeEditor1.Nodes[1].LockOption = !stNodeEditor1.Nodes[1].LockOption;


            stNodeEditor1.Nodes[0].LockLocation = !stNodeEditor1.Nodes[0].LockLocation;
            stNodeEditor1.Nodes[1].LockLocation = !stNodeEditor1.Nodes[1].LockLocation;
        }

        private void button2_Click(object sender, EventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "*.stn|*.stn";
            if (sfd.ShowDialog() != DialogResult.OK) return;
            stNodeEditor1.SaveCanvas(sfd.FileName);
        }

        private void button3_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.stn|*.stn";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            stNodeEditor1.LoadCanvas(ofd.FileName);
            stNodeEditor1.Nodes.Clear();
        }
    }
}
