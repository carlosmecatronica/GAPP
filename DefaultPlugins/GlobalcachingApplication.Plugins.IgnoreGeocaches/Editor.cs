﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlobalcachingApplication.Plugins.IgnoreGeocaches
{
    public class Editor : Utils.BasePlugin.BaseUIChildWindow
    {
        public const string ACTION_SHOW = "Edit ignored geocaches";

        public async override Task<bool> InitializeAsync(Framework.Interfaces.ICore core)
        {
            if (PluginSettings.Instance == null)
            {
                var p = new PluginSettings(core);
            }

            AddAction(ACTION_SHOW);

            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_TITLE));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_ADD));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_EXPLAIN));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_GCCODE));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_GCNAME));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_GCOWNER));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_PROPERTY));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_REMOVE));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_VALUES));
            core.LanguageItems.Add(new Framework.Data.LanguageItem(EditorForm.STR_CLEARALL));

            return await base.InitializeAsync(core);
        }

        public override string DefaultAction
        {
            get
            {
                return ACTION_SHOW;
            }
        }

        protected override Utils.BasePlugin.BaseUIChildWindowForm CreateUIChildWindowForm(Framework.Interfaces.ICore core)
        {
            return (new EditorForm(this, core));
        }

        public override bool Action(string action)
        {
            bool result = base.Action(action);
            if (result)
            {
                if (UIChildWindowForm != null)
                {
                    if (action == ACTION_SHOW)
                    {
                        if (!UIChildWindowForm.Visible)
                        {
                            (UIChildWindowForm as EditorForm).UpdateView();
                            UIChildWindowForm.Show();
                        }
                        if (UIChildWindowForm.WindowState == FormWindowState.Minimized)
                        {
                            UIChildWindowForm.WindowState = FormWindowState.Normal;
                        }
                        UIChildWindowForm.BringToFront();
                    }
                }
                result = true;
            }
            return result;
        }

    }
}
