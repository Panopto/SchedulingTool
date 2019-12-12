using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PanoptoScheduleUploader.Services
{
    public partial class FolderChooser : Form
    {
        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        private RadioButton[] options;
        private StringBuilder folderHolder;
        public FolderChooser(string[] folderOptions, StringBuilder folderHolder, string collidedName)
        {
            InitializeComponent();
            this.folderHolder = folderHolder;
            Height = 30 + selectButton.Height*2 + bottomLabel.Height + topLabel.Height;
            options = new RadioButton[folderOptions.Length];
            for (int i = 0; i < folderOptions.Length; ++i)
            {
                options[i] = new RadioButton();
                options[i].Text = folderOptions[i];
                options[i].Width = Width;
                options[i].Location = new System.Drawing.Point(10, 10 + i * 20 + topLabel.Height);
                Height += 20;
                this.Controls.Add(options[i]);
            }
            this.topLabel.Text = "There are multiple folders named \"" + collidedName + "\". Please choose one:";
            this.topLabel.Location = new System.Drawing.Point(Width / 2 - topLabel.Width / 2, 10);
            this.selectButton.Location = new System.Drawing.Point(Width / 2 - selectButton.Width / 2,Height - 20 - selectButton.Height*2 - bottomLabel.Height);
            this.bottomLabel.Location = new System.Drawing.Point(Width / 2 - bottomLabel.Width / 2, Height - 20 - bottomLabel.Height*2);
        }

        private void selectButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < options.Length; ++i)
            {
                if (options[i].Checked == true)
                {
                    exitWithChoice(i);
                }
            }
        }

        private void exitWithChoice(int i)
        {
            folderHolder.Append(i);
            Close();
        }
    }
}
