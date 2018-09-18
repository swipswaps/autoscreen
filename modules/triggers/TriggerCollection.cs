﻿//-----------------------------------------------------------------------
// <copyright file="TriggerCollection.cs" company="Gavin Kendall">
//     Copyright (c) Gavin Kendall. All rights reserved.
// </copyright>
// <author>Gavin Kendall</author>
// <summary></summary>
//-----------------------------------------------------------------------
namespace AutoScreenCapture
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Text;
    using System.Xml;

    public static class TriggerCollection
    {
        private static ArrayList _triggerList = new ArrayList();

        private const string XML_FILE = "triggers.xml";
        private const string XML_FILE_INDENT_CHARS = "   ";
        private const string XML_FILE_TRIGGER_NODE = "trigger";
        private const string XML_FILE_TRIGGERS_NODE = "triggers";
        private const string XML_FILE_ROOT_NODE = "autoscreen";

        private const string TRIGGER_NAME = "name";
        private const string TRIGGER_XPATH = "/" + XML_FILE_ROOT_NODE + "/" + XML_FILE_TRIGGERS_NODE + "/" + XML_FILE_TRIGGER_NODE;

        public static void Add(Trigger trigger)
        {
            _triggerList.Add(trigger);

            Log.Write("Added " + trigger.Name);
        }

        public static void Remove(Trigger trigger)
        {
            _triggerList.Remove(trigger);

            Log.Write("Removed " + trigger.Name);
        }

        public static int Count
        {
            get { return _triggerList.Count; }
        }

        public static Trigger Get(Trigger triggerToFind)
        {
            for (int i = 0; i < _triggerList.Count; i++)
            {
                Trigger trigger = GetByIndex(i);

                if (trigger.Equals(triggerToFind))
                {
                    return GetByIndex(i);
                }
            }

            return null;
        }

        public static Trigger GetByIndex(int index)
        {
            return (Trigger)_triggerList[index];
        }

        public static Trigger GetByName(string name)
        {
            for (int i = 0; i < _triggerList.Count; i++)
            {
                Trigger trigger = GetByIndex(i);

                if (trigger.Name.Equals(name))
                {
                    return GetByIndex(i);
                }
            }

            return null;
        }

        public static void Load()
        {
            if (File.Exists(FileSystem.UserAppDataLocalDirectory + XML_FILE))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(FileSystem.UserAppDataLocalDirectory + XML_FILE);

                XmlNodeList xTriggers = xDoc.SelectNodes(TRIGGER_XPATH);

                foreach (XmlNode xTrigger in xTriggers)
                {
                    Trigger trigger = new Trigger();
                    XmlNodeReader xReader = new XmlNodeReader(xTrigger);

                    while (xReader.Read())
                    {
                        if (xReader.IsStartElement())
                        {
                            switch (xReader.Name)
                            {
                                case TRIGGER_NAME:
                                    xReader.Read();
                                    trigger.Name = xReader.Value;
                                    break;
                            }
                        }
                    }

                    xReader.Close();

                    if (!string.IsNullOrEmpty(trigger.Name))
                    {
                        Add(trigger);
                    }
                }
            }
        }

        public static void Save()
        {
            XmlWriterSettings xSettings = new XmlWriterSettings();
            xSettings.Indent = true;
            xSettings.CloseOutput = true;
            xSettings.CheckCharacters = true;
            xSettings.Encoding = Encoding.UTF8;
            xSettings.NewLineChars = Environment.NewLine;
            xSettings.IndentChars = XML_FILE_INDENT_CHARS;
            xSettings.NewLineHandling = NewLineHandling.Entitize;
            xSettings.ConformanceLevel = ConformanceLevel.Document;

            using (XmlWriter xWriter = XmlWriter.Create(FileSystem.UserAppDataLocalDirectory + XML_FILE, xSettings))
            {
                xWriter.WriteStartDocument();
                xWriter.WriteStartElement(XML_FILE_ROOT_NODE);
                xWriter.WriteStartElement(XML_FILE_TRIGGERS_NODE);

                foreach (object obj in _triggerList)
                {
                    Trigger trigger = (Trigger)obj;

                    xWriter.WriteStartElement(XML_FILE_TRIGGER_NODE);
                    xWriter.WriteElementString(TRIGGER_NAME, trigger.Name);

                    xWriter.WriteEndElement();
                }

                xWriter.WriteEndElement();
                xWriter.WriteEndElement();
                xWriter.WriteEndDocument();

                xWriter.Flush();
                xWriter.Close();
            }
        }
    }
}
