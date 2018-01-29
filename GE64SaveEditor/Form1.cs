using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace GE64SaveEditor
{
    public partial class Form1 : Form
    {
        GE64SaveGame save;
        List<uint> other;
        List<ushort> timesAgent;
        List<ushort> timesSAgent;
        List<ushort> times00Agent;
        uint cheats;
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.eep|*.eep";
            if(d.ShowDialog() == DialogResult.OK)
            {
                save = new GE64SaveGame(d.FileName);
                RefreshList();
            }
        }

        public void RefreshList()
        {
            listBox1.Items.Clear();
            if (save == null || !save.isValid) return;
            listBox1.Items.Add("Slot 1");
            listBox1.Items.Add("Slot 2");
            listBox1.Items.Add("Slot 3");
            listBox1.Items.Add("Slot 4");
            listBox1.Items.Add("Slot 5");
            comboBox1.Items.Clear();
            foreach (string key in GE64SaveGame.cheatFlags.Keys)
                comboBox1.Items.Add(key);
            comboBox1.SelectedIndex = 0;
        }

        public void RefreshSlot()
        {
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            listBox4.Items.Clear();
            for (int i = 0; i < 20; i++)
            {
                listBox2.Items.Add(save.getTimeString(timesAgent[i], i));
                listBox3.Items.Add(save.getTimeString(timesSAgent[i], i));
                listBox4.Items.Add(save.getTimeString(times00Agent[i], i));
            }
            textBox1.Text = other[0].ToString("X4");
            trackBar1.Value = (byte)(other[1] >> 8);
            trackBar2.Value = (byte)(other[1] & 0xFF);
            textBox3.Text = other[2].ToString("X4");
            cheats = other[3];
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1 || save == null || !save.isValid) return;
            timesAgent = save.getSlotTimes(n, 0);
            timesSAgent = save.getSlotTimes(n, 1);
            times00Agent = save.getSlotTimes(n, 2);
            other = save.getOther(n);
            RefreshSlot();
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1 || save == null || !save.isValid) return;
            string s = Interaction.InputBox("Please enter new time in seconds", "Edit Time", timesAgent[n].ToString());
            if (s == "") return;
            timesAgent[n] = Convert.ToUInt16(s);
            RefreshSlot();
        }

        private void listBox3_DoubleClick(object sender, EventArgs e)
        {
            int n = listBox3.SelectedIndex;
            if (n == -1 || save == null || !save.isValid) return;
            string s = Interaction.InputBox("Please enter new time in seconds", "Edit Time", timesSAgent[n].ToString());
            if (s == "") return;
            timesSAgent[n] = Convert.ToUInt16(s);
            RefreshSlot();
        }

        private void listBox4_DoubleClick(object sender, EventArgs e)
        {
            int n = listBox4.SelectedIndex;
            if (n == -1 || save == null || !save.isValid) return;
            string s = Interaction.InputBox("Please enter new time in seconds", "Edit Time", times00Agent[n].ToString());
            if (s == "") return;
            times00Agent[n] = Convert.ToUInt16(s);
            RefreshSlot();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1 || save == null || !save.isValid) return;
            other = new List<uint>();
            other.Add(Convert.ToUInt16(textBox1.Text, 16));
            ushort volumes = (byte)trackBar1.Value;
            volumes <<= 8;
            volumes |= (byte)trackBar2.Value;
            other.Add(volumes);
            other.Add(Convert.ToUInt16(textBox3.Text, 16));
            other.Add(cheats);
            save.makeSlot(n, timesAgent, timesSAgent, times00Agent, other);
            RefreshSlot();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (save == null || !save.isValid) return;
            save.Save();
            MessageBox.Show("Done.");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (save == null || !save.isValid) return;
            uint flag = GE64SaveGame.cheatFlags[comboBox1.SelectedItem.ToString()];
            checkBox1.Checked = (cheats & flag) != 0;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (save == null || !save.isValid) return;
            uint flag = GE64SaveGame.cheatFlags[comboBox1.SelectedItem.ToString()];
            if (checkBox1.Checked)
                cheats |= flag;
            else
                cheats &= (0xFFFFFFFF ^ flag);
        }
    }
}
