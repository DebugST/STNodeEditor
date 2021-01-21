using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ST.Library.UI
{
    public partial class Form2 : Form
    {
        public Form2() {
            InitializeComponent();
        }

        Graphics m_g;

        private void button1_Click(object sender, EventArgs e) {
            m_g = this.CreateGraphics();
        }

        private void button2_Click(object sender, EventArgs e) {
            m_g.DrawRectangle(Pens.Red, 10, 10, 30, 30);
        }

        private void button3_Click(object sender, EventArgs e) {
            m_g.DrawRectangle(Pens.Yellow, 45, 45, 20, 20);
        }

        private void button4_Click(object sender, EventArgs e) {
            this.CreateGraphics().FillRectangle(Brushes.Black, 20, 20, 20, 20);
        }
    }
}
