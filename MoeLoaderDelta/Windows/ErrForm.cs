using System.Drawing;
using System.Windows.Forms;

namespace MoeLoaderDelta
{
    public partial class ErrForm : Form
    {
        public ErrForm(string err)
        {
            InitializeComponent();

            textBox1.Text = err;
            pictureBox1.Image = SystemIcons.Error.ToBitmap();
        }
    }
}
