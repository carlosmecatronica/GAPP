﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GlobalcachingApplication.Plugins.GCView
{
    public partial class SettingsPanel : UserControl
    {
        private GeocacheViewerForm _viewer;

        public SettingsPanel()
        {
            InitializeComponent();
        }

        public SettingsPanel(GeocacheViewerForm viewer)
        {
            InitializeComponent();

            _viewer = viewer;

            comboBoxTemplates.Items.Add(Utils.LanguageSupport.Instance.GetTranslation("Geocache template"));
            comboBoxTemplates.Items.Add(Utils.LanguageSupport.Instance.GetTranslation("Log even template"));
            comboBoxTemplates.Items.Add(Utils.LanguageSupport.Instance.GetTranslation("Log odd template"));
            comboBoxTemplates.SelectedIndex = 0;
        }

        private void buttonEditTemplate_Click(object sender, EventArgs e)
        {
            using (EditTemplateForm dlg = new EditTemplateForm())
            {
                string s = "";
                switch (comboBoxTemplates.SelectedIndex)
                {
                    case 0:
                        if (string.IsNullOrEmpty(PluginSettings.Instance.GeocacheTemplateHtml))
                        {
                            s = _viewer._defaultGeocacheTemplateHtml;
                        }
                        else
                        {
                            s = PluginSettings.Instance.GeocacheTemplateHtml;
                        }                        
                        break;
                    case 1:
                        if (string.IsNullOrEmpty(PluginSettings.Instance.LogTemplateEvenHtml))
                        {
                            s = _viewer._defaultLogEvenTemplateHtml;
                        }
                        else
                        {
                            s = PluginSettings.Instance.LogTemplateEvenHtml;
                        }
                        break;
                    case 2:
                        if (string.IsNullOrEmpty(PluginSettings.Instance.LogTemplateOddHtml))
                        {
                            s = _viewer._defaultLogOddTemplateHtml;
                        }
                        else
                        {
                            s = PluginSettings.Instance.LogTemplateOddHtml;
                        }
                        break;
                }
                dlg.textBoxTemplate.Text = s;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    s = dlg.textBoxTemplate.Text;
                    switch (comboBoxTemplates.SelectedIndex)
                    {
                        case 0:
                            PluginSettings.Instance.GeocacheTemplateHtml = s;
                            break;
                        case 1:
                            PluginSettings.Instance.LogTemplateEvenHtml = s;
                            break;
                        case 2:
                            PluginSettings.Instance.LogTemplateOddHtml = s;
                            break;
                    }
                }
            }
        }
    }

}
