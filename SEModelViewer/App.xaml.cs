// ------------------------------------------------------------------------
// SEModelViewer - Tool to view SEModel Files
// Copyright (C) 2018 Philip/Scobalula
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
