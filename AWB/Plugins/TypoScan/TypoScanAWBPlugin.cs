﻿using System;
using System.Collections.Generic;
using System.Text;

using WikiFunctions;
using WikiFunctions.Plugin;

namespace WikiFunctions.Plugins.ListMaker.TypoScan
{
    class TypoScanAWBPlugin : IAWBPlugin
    {
        #region IAWBPlugin Members

        internal static IAutoWikiBrowser AWB;

        internal static Dictionary<string, int> PageList = new Dictionary<string, int>();

        static List<int> FinishedPages = new List<int>();

        public void Initialise(IAutoWikiBrowser sender)
        {
            AWB = sender;
            AWB.LogControl.LogAdded += new WikiFunctions.Logging.LogControl.LogAddedToControl(LogControl_LogAdded);
        }

        void LogControl_LogAdded(bool Skipped, WikiFunctions.Logging.AWBLogListener LogListener)
        {
            //DoSomething
            if (PageList.ContainsKey(LogListener.Text))
            {
                int articleID;
                PageList.TryGetValue(LogListener.Text, out articleID);

                FinishedPages.Add(articleID);
            }
        }

        public string Name
        {
            get { return "TypoScan AWB Plugin]]"; }
        }

        public string WikiName
        {
            get
            {
                return "[[Wikipedia:TypoScan|TypoScan AWB Plugin]], Plugin version " +
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string ProcessArticle(IAutoWikiBrowser sender, ProcessArticleEventArgs eventargs)
        {
            return eventargs.ArticleText;
        }

        public void LoadSettings(object[] prefs)
        { }

        public object[] SaveSettings()
        {
            return new object[0];
        }

        public void Reset()
        { }

        public void Nudge(out bool Cancel)
        {
            Cancel = false;
        }

        public void Nudged(int Nudges)
        { }

        #endregion
    }
}
