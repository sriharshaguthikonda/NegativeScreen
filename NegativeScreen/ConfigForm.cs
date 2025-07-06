using System;
using System.Windows.Forms;

namespace NegativeScreen
{
    public partial class ConfigForm : Form
    {
        private Config config;
        public ConfigForm(Config config)
        {
            InitializeComponent();
            this.config = config;
            this.numericUpDownInterval.Value = config.RefreshInterval;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            config.RefreshInterval = (int)this.numericUpDownInterval.Value;
            config.Save();
            this.Close();
        }
    }
}
