﻿//////////////////////////////////////////////////////////
// Auto Screen Capture 2.1.0
// autoscreen.StatusMessage.cs
//
// Developed by Gavin Kendall
// Thursday, 15 May 2008 - Monday, 12 March 2018

namespace autoscreen
{
    internal class StatusMessage
    {
        internal const string ON = "On";
        internal const string OFF = "Off";
        internal const string RUNNING = "Running";
        internal const string STOPPED = "Stopped";
        internal const string LAST_CAPTURE_APP = "%year%-%month%-%day% %hour%:%minute%:%second%.%millisecond% (%format%)";
        internal const string LAST_CAPTURE_ICON = "Last capture: %year%-%month%-%day% %hour%:%minute%:%second%.%millisecond% (%format%)";
    }
}
