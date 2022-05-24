// ------------------------------------------------------------------------
// SEModelViewer - Tool to view SEModel Files
// Copyright (C) 2018 Philip/Scobalula
// ------------------------------------------------------------------------
#define TRACE
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace SEModelViewer.Util
{
    /// <summary>
    /// SEModelViewer Settings
    /// Contains methods for working with settings
    /// </summary>
    class Settings
    {
        /// <summary>
        /// Setting Values
        /// </summary>
        private static Dictionary<string, string> Values = new Dictionary<string, string>();

        /// <summary>
        /// Gets a setting
        /// </summary>
        /// <param name="setting">Setting Name</param>
        /// <param name="defaultValue">Default Value to return if setting is not found (null if not set)</param>
        /// <returns>Setting value, otherwise default value (null if not set)</returns>
        public static string Get(string setting, string defaultValue = null)
        {
            return Values.TryGetValue(setting, out string value) ? value : defaultValue;
        }

        /// <summary>
        /// Sets a settings
        /// </summary>
        /// <param name="setting">Setting Name</param>
        /// <param name="value">Value to assign</param>
        public static void Set(string setting, string value)
        {
            Values[setting] = value;
        }

        /// <summary>
        /// Loads Settings from a file
        /// </summary>
        /// <param name="fileName">File Name</param>
        public static void Load(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    Save(fileName);
                }
                else
                {
                    using (var reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
                    {
                        int count = reader.ReadInt32();

                        for(int i = 0; i < count; i++)
                            Values[reader.ReadString()] = reader.ReadString();
                    }
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine(e);
                return;
            }
        }

        /// <summary>
        /// Saves all settings to a file
        /// </summary>
        /// <param name="fileName">File Name</param>
        public static void Save(string fileName)
        {
            try
            {
                using (var writer = new BinaryWriter(new FileStream(fileName, FileMode.Create)))
                {
                    writer.Write(Values.Count);

                    foreach(var value in Values)
                    {
                        writer.Write(value.Key);
                        writer.Write(value.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                return;
            }
        }
    }
}
