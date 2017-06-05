using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MoeLoaderDelta
{
    public partial class ErrForm : Form
    {
        public ErrForm(string err)
        {
            InitializeComponent();

            Text = MainWindow.ProgramName + " - Fatal Error";
            label1.Text.Replace("<AN>", MainWindow.ProgramName);
            textBox1.Text = err;
            pictureBox1.Image = SystemIcons.Error.ToBitmap();
        }
    }
}
