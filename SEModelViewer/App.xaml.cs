﻿// ------------------------------------------------------------------------
// SEModelViewer - Tool to view SEModel Files
// Copyright (C) 2018 Philip/Scobalula
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ------------------------------------------------------------------------
using System;
using System.IO;
using System.Windows;

namespace SEModelViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Checks if we have models to process on startup
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            var activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments?.ActivationData;

            if (activationData != null && activationData.Length > 0)
            {
                string path = new Uri(activationData[0]).LocalPath;

                if(File.Exists(path) && Path.GetExtension(path) == ".semodel")
                    Properties["SEModelInput"] = path;
            }
            else if (e.Args != null && e.Args.Length > 0)
            {
                string path = e.Args[0];

                if (File.Exists(path) && Path.GetExtension(path) == ".semodel")
                    Properties["SEModelInput"] = path;
            }

            base.OnStartup(e);
        }
    }
}
