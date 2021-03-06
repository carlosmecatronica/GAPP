﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GAPPSF.Core
{
    public partial class Settings
    {
        public string SettingsFolder
        {
            get { return GetProperty(null); }
            set { SetProperty(value); }
        }

        public string SelectedCulture
        {
            get { return GetProperty(null); }
            set { SetProperty(value); }
        }

        public string OpenedDatabases
        {
            get { return GetProperty(null); }
            set { SetProperty(value); }
        }

        public string ActiveDatabase
        {
            get { return GetProperty(null); }
            set { SetProperty(value); }
        }

        public string ActiveGeocache
        {
            get { return GetProperty(null); }
            set { SetProperty(value); }
        }

        public bool FirstStart
        {
            get { return bool.Parse(GetProperty("True")); }
            set { SetProperty(value.ToString()); }
        }

        public bool AutoLoadDatabases
        {
            get { return bool.Parse(GetProperty("True")); }
            set { SetProperty(value.ToString()); }
        }

        public bool AutoSelectNewGeocaches
        {
            get { return bool.Parse(GetProperty("False")); }
            set { SetProperty(value.ToString()); }
        }

        public string AccountInfos
        {
            get { return GetProperty(null); }
            set { SetProperty(value); }
        }

        public double HomeLocationLat
        {
            get { return double.Parse(GetProperty("0.0"), CultureInfo.InvariantCulture); }
            set { SetProperty(value.ToString(CultureInfo.InvariantCulture)); }
        }
        public double HomeLocationLon
        {
            get { return double.Parse(GetProperty("0.0"), CultureInfo.InvariantCulture); }
            set { SetProperty(value.ToString(CultureInfo.InvariantCulture)); }
        }

        public double CenterLocationLat
        {
            get { return double.Parse(GetProperty("0.0"), CultureInfo.InvariantCulture); }
            set { SetProperty(value.ToString(CultureInfo.InvariantCulture)); }
        }
        public double CenterLocationLon
        {
            get { return double.Parse(GetProperty("0.0"), CultureInfo.InvariantCulture); }
            set { SetProperty(value.ToString(CultureInfo.InvariantCulture)); }
        }

    }
}
