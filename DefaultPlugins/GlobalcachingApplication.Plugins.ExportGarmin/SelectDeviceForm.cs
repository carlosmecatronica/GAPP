﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GlobalcachingApplication.Plugins.ExportGarmin
{
    public partial class SelectDeviceForm : Form
    {
        public const string STR_TITLE = "Select Garmin Device";
        public const string STR_OK = "OK";
        public const string STR_SELECTDEVICE = "Select device";
        public const string STR_ADDCHILDWAYPOINTS = "Add child waypoints";
        public const string STR_SEPFILEPERGEOCACHE = "Seperate file per geocache";
        public const string STR_USENAME = "Use name and not geocache code";
        public const string STR_ADDWPTTODESCR = "Add additional waypoints to description";
        public const string STR_USEHINTSDESCR = "Use the hints for description";
        public const string STR_USEDATABASENAME = "Use database name as GPX file name on device";
        public const string STR_CREATEGGZFILE = "Create GGZ file";
        public const string STR_INCLNOTES = "Include notes in description";
        public const string STR_GPXVERSION = "GPX version";
        public const string STR_MAXNAMELENGTH = "Maximum geocache name length";
        public const string STR_MINSTARTNAME = "Minimum start of name length";
        public const string STR_EXTRACOORDNAMEPREFIX = "Extra coord. name prefix";
        public const string STR_ADDIMAGES = "Add images";
        public const string STR_EXTRAINFO = "Add extra information to description";
        public const string STR_MAXLOGS = "Maximum number of logs";

        private Utils.Devices.GarminMassStorage _garminmsDev = null;

        public SelectDeviceForm()
        {
            InitializeComponent();

            this.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_TITLE);
            this.label1.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_SELECTDEVICE);
            this.label4.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_GPXVERSION);
            this.button1.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_OK);
            this.checkBox1.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_SEPFILEPERGEOCACHE);
            this.checkBox2.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_ADDCHILDWAYPOINTS);
            this.checkBox3.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_USENAME);
            this.checkBox4.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_ADDWPTTODESCR);
            this.checkBox5.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_USEHINTSDESCR);
            this.checkBox6.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_USEDATABASENAME);
            this.checkBox7.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_CREATEGGZFILE);
            this.checkBox8.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_INCLNOTES);
            this.checkBox9.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_ADDIMAGES);
            this.label8.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_MAXNAMELENGTH);
            this.label7.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_MINSTARTNAME);
            this.label10.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_EXTRACOORDNAMEPREFIX);
            this.label12.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_MAXLOGS);
            this.checkBox10.Text = Utils.LanguageSupport.Instance.GetTranslation(STR_EXTRAINFO);

            numericUpDown1.Value = PluginSettings.Instance.MaxGeocacheNameLength;
            numericUpDown2.Value = PluginSettings.Instance.MinStartOfGeocacheName;
            numericUpDown3.Value = PluginSettings.Instance.MaximumNumberOfLogs;
            checkBox1.Checked = PluginSettings.Instance.SeperateFilePerGeocache;
            checkBox2.Checked = PluginSettings.Instance.AddChildWaypoints;
            checkBox3.Checked = PluginSettings.Instance.UseNameAndNotCode;
            checkBox4.Checked = PluginSettings.Instance.AddWaypointsToDescription;
            checkBox5.Checked = PluginSettings.Instance.UseHintsForDescription;
            checkBox6.Checked = PluginSettings.Instance.UseDatabaseNameForFileName;
            checkBox6.Enabled = !checkBox1.Checked;
            checkBox7.Checked = PluginSettings.Instance.CreateGGZFile;
            checkBox1.Enabled = !checkBox7.Checked;
            checkBox8.Checked = PluginSettings.Instance.AddFieldNotesToDescription;
            textBox1.Text = PluginSettings.Instance.CorrectedNamePrefix ?? "";
            checkBox9.Checked = PluginSettings.Instance.AddImages;
            checkBox10.Checked = PluginSettings.Instance.AddExtraInfoToDescription;

            comboBox2.Items.Add(Utils.GPXGenerator.V100);
            comboBox2.Items.Add(Utils.GPXGenerator.V101);
            comboBox2.Items.Add(Utils.GPXGenerator.V102);
            if (!string.IsNullOrEmpty(PluginSettings.Instance.GPXVersionStr))
            {
                comboBox2.SelectedItem = Version.Parse(PluginSettings.Instance.GPXVersionStr);
            }
            else
            {
                comboBox2.SelectedIndex = 0;
            }

            _garminmsDev = new Utils.Devices.GarminMassStorage();

            _garminmsDev.DeviceAddedEvent += new EventHandler<Utils.Devices.GarminMassStorage.DeviceInfoEventArgs>(Instance_DeviceAddedEvent);
            _garminmsDev.DeviceRemovedEvent += new EventHandler<Utils.Devices.GarminMassStorage.DeviceInfoEventArgs>(Instance_DeviceRemovedEvent);

            _garminmsDev.ScanForDevices();
            var dev = _garminmsDev.ConnectedDevices;
            if (dev != null)
            {
                comboBox1.Items.AddRange(dev.ToArray());
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        public string SelectedDrive
        {
            get
            {
                string result = null;
                var dev = comboBox1.SelectedItem as Utils.Devices.GarminMassStorage.DeviceInfo;
                if (dev != null)
                {
                    result = dev.DriveName;
                }
                return result;
            }
        }

        public bool SeperateFilePerGeocache
        {
            get { return checkBox1.Checked; }
        }

        public bool AddChildWaypoints
        {
            get { return checkBox2.Checked; }
        }

        public bool UseName
        {
            get { return checkBox3.Checked; }
        }

        void Instance_DeviceRemovedEvent(object sender, Utils.Devices.GarminMassStorage.DeviceInfoEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler<Utils.Devices.GarminMassStorage.DeviceInfoEventArgs>(this.Instance_DeviceRemovedEvent), new object[] { sender, e });
                return;
            }
            Utils.Devices.GarminMassStorage.DeviceInfo dev = (from Utils.Devices.GarminMassStorage.DeviceInfo d in comboBox1.Items where d.DriveName == e.Device.DriveName select d).FirstOrDefault();
            if (dev != null)
            {
                comboBox1.Items.Remove(dev);
                if (comboBox1.SelectedIndex < 0 && comboBox1.Items.Count > 0)
                {
                    comboBox1.SelectedIndex = 0;
                }
            }
        }

        void Instance_DeviceAddedEvent(object sender, Utils.Devices.GarminMassStorage.DeviceInfoEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler<Utils.Devices.GarminMassStorage.DeviceInfoEventArgs>(this.Instance_DeviceAddedEvent), new object[] { sender, e });
                return;
            }
            comboBox1.Items.Add(e.Device);
            if (comboBox1.SelectedIndex < 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            PluginSettings.Instance.MaxGeocacheNameLength = (int)numericUpDown1.Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            PluginSettings.Instance.MinStartOfGeocacheName = (int)numericUpDown2.Value;
        }

        private void SelectDeviceForm_VisibleChanged(object sender, EventArgs e)
        {
            if (!this.Visible)
            {
                if (_garminmsDev != null)
                {
                    _garminmsDev.Dispose();
                    _garminmsDev = null;
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = comboBox1.SelectedIndex >= 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PluginSettings.Instance.SeperateFilePerGeocache = checkBox1.Checked;
            PluginSettings.Instance.AddChildWaypoints = checkBox2.Checked;
            PluginSettings.Instance.UseNameAndNotCode = checkBox3.Checked;
            PluginSettings.Instance.AddWaypointsToDescription = checkBox4.Checked;
            PluginSettings.Instance.UseHintsForDescription = checkBox5.Checked;
            PluginSettings.Instance.UseDatabaseNameForFileName = checkBox6.Checked;
            PluginSettings.Instance.CreateGGZFile = checkBox7.Checked;
            PluginSettings.Instance.AddFieldNotesToDescription = checkBox8.Checked;
            PluginSettings.Instance.CorrectedNamePrefix = textBox1.Text;
            PluginSettings.Instance.AddImages = checkBox9.Checked;
            PluginSettings.Instance.AddExtraInfoToDescription = checkBox10.Checked;
            if (comboBox2.SelectedItem as Version == null)
            {
                PluginSettings.Instance.GPXVersionStr = "";
            }
            else
            {
                PluginSettings.Instance.GPXVersionStr = (comboBox2.SelectedItem as Version).ToString();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox6.Checked = false;
            checkBox6.Enabled = !checkBox1.Checked;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            checkBox1.Enabled = !checkBox7.Checked;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            PluginSettings.Instance.MaximumNumberOfLogs = (int)numericUpDown3.Value;
        }
    }
}
