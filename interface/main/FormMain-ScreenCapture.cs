﻿//-----------------------------------------------------------------------
// <copyright file="FormMain-ScreenCapture.cs" company="Gavin Kendall">
//     Copyright (c) 2020 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>All the methods related to capturing screens and what we should do when buttons are clicked or menu items are selected.</summary>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace AutoScreenCapture
{
    public partial class FormMain : Form
    {
        private FormRegionSelectWithMouse _formRegionSelectWithMouse;

        /// <summary>
        /// Returns the screen capture interval. This value will be used as the screen capture timer's interval property.
        /// </summary>
        /// <returns></returns>
        private int GetScreenCaptureInterval()
        {
            return ConvertIntoMilliseconds((int)numericUpDownHoursInterval.Value,
                (int)numericUpDownMinutesInterval.Value, (int)numericUpDownSecondsInterval.Value,
                (int)numericUpDownMillisecondsInterval.Value);
        }

        /// <summary>
        /// Figures out if the "Start Capture" controls should be enabled or disabled.
        /// </summary>
        private void EnableStartCapture()
        {
            if (GetScreenCaptureInterval() > 0)
            {
                toolStripSplitButtonStartScreenCapture.Enabled = true;
                toolStripMenuItemStartScreenCapture.Enabled = true;

                groupBoxCaptureDelay.Enabled = true;

                labelLimit.Enabled = true;
                checkBoxCaptureLimit.Enabled = true;

                numericUpDownCaptureLimit.Enabled = true;
                numericUpDownSecondsInterval.Enabled = true;
                numericUpDownMillisecondsInterval.Enabled = true;

                checkBoxScreenshotLabel.Enabled = true;
                comboBoxScreenshotLabel.Enabled = true;

                groupBoxActiveWindowTitle.Enabled = true;

                groupBoxApplicationFocus.Enabled = true;
            }
            else
            {
                DisableStartCapture();
            }
        }

        /// <summary>
        /// Enables the "Stop Capture" controls.
        /// </summary>
        private void EnableStopScreenCapture()
        {
            toolStripSplitButtonStopScreenCapture.Enabled = true;
            toolStripMenuItemStopScreenCapture.Enabled = true;

            groupBoxCaptureDelay.Enabled = false;

            labelLimit.Enabled = false;
            checkBoxCaptureLimit.Enabled = false;

            numericUpDownCaptureLimit.Enabled = false;
            numericUpDownSecondsInterval.Enabled = false;
            numericUpDownMillisecondsInterval.Enabled = false;

            checkBoxScreenshotLabel.Enabled = false;
            comboBoxScreenshotLabel.Enabled = false;

            groupBoxActiveWindowTitle.Enabled = false;

            groupBoxApplicationFocus.Enabled = false;
        }

        /// <summary>
        /// Disables the "Stop Capture" controls.
        /// </summary>
        private void DisableStopCapture()
        {
            toolStripSplitButtonStopScreenCapture.Enabled = false;
            toolStripMenuItemStopScreenCapture.Enabled = false;
        }

        /// <summary>
        /// Disables the "Start Capture" controls.
        /// </summary>
        private void DisableStartCapture()
        {
            toolStripSplitButtonStartScreenCapture.Enabled = false;
            toolStripMenuItemStartScreenCapture.Enabled = false;
        }

        /// <summary>
        /// Checks the capture limit when the checkbox is selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckedChanged_checkBoxCaptureLimit(object sender, EventArgs e)
        {
            CaptureLimitCheck();
        }

        /// <summary>
        /// Checks the capture limit.
        /// </summary>
        private void CaptureLimitCheck()
        {
            if (checkBoxCaptureLimit.Checked)
            {
                numericUpDownCaptureLimit.Enabled = true;

                _screenCapture.Count = 0;
                _screenCapture.Limit = (int)numericUpDownCaptureLimit.Value;
            }
            else
            {
                numericUpDownCaptureLimit.Enabled = false;
            }
        }

        /// <summary>
        /// Takes a screenshot of each available region and screen.
        /// </summary>
        private void TakeScreenshot(bool captureNow)
        {
            RunTriggersOfConditionType(TriggerConditionType.BeforeScreenshotTaken);

            // Test to see if we can get images of the screen before continuing.
            if (_screenCapture.GetScreenImages(0, 0, 0, 0, 0, 0, false, 100, out _))
            {
                MacroParser.screenCapture = _screenCapture;

                _screenCapture.Count++;
                _screenCapture.CaptureNow = captureNow;

                DateTime dtNow = DateTime.Now;

                _screenCapture.DateTimeScreenshotsTaken = dtNow;

                if (!captureNow)
                {
                    _screenCapture.DateTimePreviousCycle = dtNow;
                }

                DoApplicationFocus();

                _screenCapture.ActiveWindowTitle = _screenCapture.GetActiveWindowTitle();

                _screenCapture.ActiveWindowProcessName = _screenCapture.GetActiveWindowProcessName();

                // Do not continue if the active window title needs to be checked and the active window title does not contain the defined text or regex pattern.
                if (checkBoxActiveWindowTitle.Checked && !ActiveWindowTitleMatchesText())
                {
                    return;
                }

                RunRegionCaptures();

                RunScreenCaptures();
            }
        }

        /// <summary>
        /// Starts a screen capture session with the default screen capture interval.
        /// </summary>
        private void StartScreenCapture()
        {
            int screenCaptureInterval = GetScreenCaptureInterval();

            StartScreenCapture(screenCaptureInterval);
        }

        /// <summary>
        /// Starts a screen capture session with a defined screen capture interval.
        /// </summary>
        /// <param name="screenCaptureInterval">The screen capture interval to use.</param>
        private void StartScreenCapture(int screenCaptureInterval)
        {
            try
            {
                Log.WriteDebugMessage("Starting a screen capture session");

                if (!_screenCapture.Running && screenCaptureInterval > 0)
                {
                    // Increment the number of times the user has started a screen capture session.
                    int startScreenCaptureCount = Convert.ToInt32(Settings.User.GetByKey("StartScreenCaptureCount", DefaultSettings.StartScreenCaptureCount).Value);
                    startScreenCaptureCount++;

                    Settings.User.SetValueByKey("StartScreenCaptureCount", startScreenCaptureCount);

                    // Turn off "FirstRun" on the first run.
                    if (startScreenCaptureCount == 1)
                    {
                        Settings.User.SetValueByKey("FirstRun", false);
                    }

                    SaveSettings();

                    // Stop the date search thread if it's busy.
                    if (runDateSearchThread != null && runDateSearchThread.IsBusy)
                    {
                        runDateSearchThread.CancelAsync();
                    }

                    // Stop the slide search thread if it's busy.
                    if (runScreenshotSearchThread != null && runScreenshotSearchThread.IsBusy)
                    {
                        runScreenshotSearchThread.CancelAsync();
                    }

                    DisableStartCapture();
                    EnableStopScreenCapture();

                    // Setup the properties for the screen capture class.
                    _screenCapture.Interval = screenCaptureInterval;
                    _screenCapture.Limit = checkBoxCaptureLimit.Checked ? (int)numericUpDownCaptureLimit.Value : 0;

                    if (Settings.User.GetByKey("Passphrase", DefaultSettings.Passphrase).Value.ToString().Length > 0)
                    {
                        ScreenCapture.LockScreenCaptureSession = true;
                    }
                    else
                    {
                        ScreenCapture.LockScreenCaptureSession = false;
                    }

                    Log.WriteMessage("Starting screen capture");

                    _screenCapture.Running = true;
                    _screenCapture.ApplicationWarning = false;

                    _screenCapture.DateTimeStartCapture = DateTime.Now;

                    if (checkBoxInitialScreenshot.Checked)
                    {
                        Log.WriteMessage("Taking initial screenshots");

                        TakeScreenshot(captureNow: false);
                    }

                    // Start taking screenshots.

                    timerScreenCapture.Interval = screenCaptureInterval;

                    timerScreenCapture.Enabled = true;
                    timerScreenCapture.Start();

                    Log.WriteDebugMessage("Running triggers of condition type ScreenCaptureStarted");
                    RunTriggersOfConditionType(TriggerConditionType.ScreenCaptureStarted);
                }
            }
            catch (Exception ex)
            {
                _screenCapture.ApplicationError = true;
                Log.WriteExceptionMessage("FormMain-ScreenCapture::StartScreenCapture", ex);
            }
        }

        /// <summary>
        /// Stops the screen capture session that's currently running.
        /// </summary>
        private void StopScreenCapture()
        {
            try
            {
                Log.WriteMessage("Stopping screen capture");

                if (ScreenCapture.LockScreenCaptureSession && !_formEnterPassphrase.Visible)
                {
                    Log.WriteDebugMessage("Screen capture session is locked. Challenging user to enter correct passphrase to unlock");
                    _formEnterPassphrase.ShowDialog(this);
                }

                // This is intentional. Do not rewrite these statements as an if/else
                // because as soon as lockScreenCaptureSession is set to false we want
                // to continue with normal functionality.
                if (!ScreenCapture.LockScreenCaptureSession)
                {
                    Settings.User.GetByKey("Passphrase", DefaultSettings.Passphrase).Value = string.Empty;
                    SaveSettings();

                    DisableStopCapture();
                    EnableStartCapture();

                    _screenCapture.Count = 0;
                    _screenCapture.Running = false;
                    _screenCapture.DateTimePreviousCycle = DateTime.MinValue;

                    timerScreenCapture.Stop();
                    timerScreenCapture.Enabled = false;

                    SearchFilterValues();
                    SearchDates();

                    Log.WriteDebugMessage("Running triggers of condition type ScreenCaptureStopped");
                    RunTriggersOfConditionType(TriggerConditionType.ScreenCaptureStopped);
                }
            }
            catch (Exception ex)
            {
                _screenCapture.ApplicationError = true;
                Log.WriteExceptionMessage("FormMain-ScreenCapture::StopScreenCapture", ex);
            }
        }

        private void CaptureNowArchive()
        {
            TakeScreenshot(captureNow: true);
        }

        private void CaptureNowEdit()
        {
            string defaultEditor = Settings.User.GetByKey("DefaultEditor", DefaultSettings.DefaultEditor).Value.ToString();

            if (string.IsNullOrEmpty(defaultEditor))
            {
                return;
            }

            TakeScreenshot(captureNow: true);

            Editor editor = _formEditor.EditorCollection.GetByName(defaultEditor);

            if (editor != null)
            {
                RunEditor(editor, TriggerActionType.RunEditor);
            }
        }

        private bool SaveScreenshot(Bitmap bitmap, Screen screen)
        {
            if (bitmap == null)
            {
                return false;
            }

            Screenshot screenshot = new Screenshot(_screenCapture.ActiveWindowTitle, _screenCapture.DateTimeScreenshotsTaken)
            {
                ViewId = screen.ViewId,
                Path = FileSystem.CorrectScreenshotsFolderPath(MacroParser.ParseTags(config: false, screen.Folder, _formMacroTag.MacroTagCollection)) + MacroParser.ParseTags(preview: false, config: false, screen.Name, screen.Macro, screen.Component, screen.Format, _screenCapture.ActiveWindowTitle, _formMacroTag.MacroTagCollection),
                Bitmap = bitmap,
                Format = screen.Format,
                ProcessName = _screenCapture.ActiveWindowProcessName + ".exe",
                Label = checkBoxScreenshotLabel.Checked ? comboBoxScreenshotLabel.Text : string.Empty
            };

            if (_screenCapture.SaveScreenshot(screen.JpegQuality, screenshot, _screenshotCollection))
            {
                ScreenshotTakenWithSuccess();

                return true;
            }
            else
            {
                ScreenshotTakenWithFailure();

                return false;
            }
        }

        private bool SaveScreenshot(Bitmap bitmap, Region region)
        {
            if (bitmap == null)
            {
                return false;
            }

            Screenshot screenshot = new Screenshot(_screenCapture.ActiveWindowTitle, _screenCapture.DateTimeScreenshotsTaken)
            {
                ViewId = region.ViewId,
                Path = FileSystem.CorrectScreenshotsFolderPath(MacroParser.ParseTags(config: false, region.Folder, _formMacroTag.MacroTagCollection)) + MacroParser.ParseTags(preview: false, config: false, region.Name, region.Macro, -1, region.Format, _screenCapture.ActiveWindowTitle, _formMacroTag.MacroTagCollection),
                Bitmap = bitmap,
                Format = region.Format,
                ProcessName = _screenCapture.ActiveWindowProcessName + ".exe",
                Label = checkBoxScreenshotLabel.Checked ? comboBoxScreenshotLabel.Text : string.Empty
            };

            if (_screenCapture.SaveScreenshot(region.JpegQuality, screenshot, _screenshotCollection))
            {
                ScreenshotTakenWithSuccess();

                return true;
            }
            else
            {
                ScreenshotTakenWithFailure();

                return false;
            }
        }

        private void ScreenshotTakenWithSuccess()
        {
            Log.WriteDebugMessage("Running triggers of condition type AfterScreenshotTaken");

            RunTriggersOfConditionType(TriggerConditionType.AfterScreenshotTaken);
        }

        private void ScreenshotTakenWithFailure()
        {
            Log.WriteMessage("Application encountered error while taking a screenshot. Stopping screen capture");

            StopScreenCapture();
        }

        /// <summary>
        /// Starts a screen capture session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemStartScreenCapture_Click(object sender, EventArgs e)
        {
            StartScreenCapture();
        }

        /// <summary>
        /// Stops the currently running screen capture session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemStopScreenCapture_Click(object sender, EventArgs e)
        {
            StopScreenCapture();
        }

        /// <summary>
        /// Takes screenshots and saves them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemCaptureNowArchive_Click(object sender, EventArgs e)
        {
            CaptureNowArchive();
        }

        /// <summary>
        /// Takes screenshots, saves them, and then opens those screenshots in the default editor. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemCaptureNowEdit_Click(object sender, EventArgs e)
        {
            CaptureNowEdit();
        }

        /// <summary>
        /// Shows a mouse-driven region selection canvas so you can select a region and then have the captured image sent to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemRegionSelectClipboard_Click(object sender, EventArgs e)
        {
            _formRegionSelectWithMouse = new FormRegionSelectWithMouse();
            _formRegionSelectWithMouse.LoadCanvas();
        }

        /// <summary>
        /// Shows a mouse-driven region selection canvas so you can select a region, have the captured image be sent to the clipboard, and then auto-saved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemRegionSelectClipboardAutoSave_Click(object sender, EventArgs e)
        {
            _formRegionSelectWithMouse = new FormRegionSelectWithMouse();
            _formRegionSelectWithMouse.MouseSelectionCompleted += _formRegionSelectWithMouse_RegionSelectClipboardAutoSaveMouseSelectionCompleted;
            _formRegionSelectWithMouse.LoadCanvas();
        }

        /// <summary>
        /// Shows a mouse-driven region selection canvas so you can select a region, have the captured image be sent to the clipboard, auto-saved, and then opened with the default image editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemRegionSelectClipboardAutoSaveEdit_Click(object sender, EventArgs e)
        {
            _formRegionSelectWithMouse = new FormRegionSelectWithMouse();
            _formRegionSelectWithMouse.MouseSelectionCompleted += _formRegionSelectWithMouse_RegionSelectClipboardAutoSaveEditMouseSelectionCompleted;
            _formRegionSelectWithMouse.LoadCanvas();
        }

        /// <summary>
        /// The event method used by "Region Select -> Clipboard / Auto Save".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _formRegionSelectWithMouse_RegionSelectClipboardAutoSaveMouseSelectionCompleted(object sender, EventArgs e)
        {
            int x = _formRegionSelectWithMouse.outputX + 1;
            int y = _formRegionSelectWithMouse.outputY + 1;
            int width = _formRegionSelectWithMouse.outputWidth - 2;
            int height = _formRegionSelectWithMouse.outputHeight - 2;

            string autoSaveFolder = textBoxAutoSaveFolder.Text;
            string autoSaveMacro = textBoxAutoSaveMacro.Text;

            ImageFormat imageFormat = new ImageFormat(ImageFormatSpec.NAME_JPEG, ImageFormatSpec.EXTENSION_JPEG);

            if (_screenCapture.GetScreenImages(-1, -1, x, y, width, height, mouse: false, resolutionRatio: 100, out Bitmap bitmap))
            {
                DateTime dtNow = DateTime.Now;

                _screenCapture.DateTimeScreenshotsTaken = dtNow;
                _screenCapture.ActiveWindowTitle = "*** Auto Screen Capture - Region Select / Auto Save ***";

                Region region = new Region()
                {
                    ViewId = new Guid(),
                    Name = string.Empty,
                    JpegQuality = 100,
                    Format = imageFormat,
                    Folder = autoSaveFolder,
                    Macro = autoSaveMacro
                };

                SaveScreenshot(bitmap, region);
            }
        }

        /// <summary>
        /// The event method for "Region Select -> Clipboard / Auto Save / Edit".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _formRegionSelectWithMouse_RegionSelectClipboardAutoSaveEditMouseSelectionCompleted(object sender, EventArgs e)
        {
            // Get the name of the default image editor.
            string defaultEditor = Settings.User.GetByKey("DefaultEditor", DefaultSettings.DefaultEditor).Value.ToString();

            if (string.IsNullOrEmpty(defaultEditor))
            {
                return;
            }

            // Save the screenshot as an image file using the Auto Save event method.
            _formRegionSelectWithMouse_RegionSelectClipboardAutoSaveMouseSelectionCompleted(sender, e);

            // Run the default image editor.
            Editor editor = _formEditor.EditorCollection.GetByName(defaultEditor);

            if (editor != null)
            {
                RunEditor(editor, TriggerActionType.RunEditor);
            }
        }

        /// <summary>
        /// Shows a folder selection dialog box for "Region Select / Auto Save".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonBrowseFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog browser = new FolderBrowserDialog();

            if (browser.ShowDialog() == DialogResult.OK)
            {
                textBoxAutoSaveFolder.Text = browser.SelectedPath;
            }
        }

        /// <summary>
        /// The timer for taking screenshots.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerScreenCapture_Tick(object sender, EventArgs e)
        {
            if (_screenCapture.Running)
            {
                if (_screenCapture.Limit >= ScreenCapture.CAPTURE_LIMIT_MIN &&
                    _screenCapture.Limit <= ScreenCapture.CAPTURE_LIMIT_MAX)
                {
                    if (_screenCapture.Count < _screenCapture.Limit)
                    {
                        TakeScreenshot(captureNow: false);
                    }

                    if (_screenCapture.Count == _screenCapture.Limit)
                    {
                        Log.WriteDebugMessage("Running triggers of condition type LimitReached");
                        RunTriggersOfConditionType(TriggerConditionType.LimitReached);
                    }
                }
                else
                {
                    TakeScreenshot(captureNow: false);
                }
            }
            else
            {
                StopScreenCapture();
            }
        }

        private bool ActiveWindowTitleMatchesText()
        {
            try
            {
                if (!string.IsNullOrEmpty(_screenCapture.ActiveWindowTitle) && !string.IsNullOrEmpty(textBoxActiveWindowTitle.Text))
                {
                    if (radioButtonCaseSensitiveMatch.Checked)
                    {
                        return _screenCapture.ActiveWindowTitle.Contains(textBoxActiveWindowTitle.Text);
                    }
                    else if (radioButtonCaseInsensitiveMatch.Checked)
                    {
                        return _screenCapture.ActiveWindowTitle.ToLower().Contains(textBoxActiveWindowTitle.Text.ToLower());
                    }
                    else if (radioButtonRegularExpressionMatch.Checked)
                    {
                        return Regex.IsMatch(_screenCapture.ActiveWindowTitle, textBoxActiveWindowTitle.Text);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteExceptionMessage("FormMain-ScreenCapture::ActiveWindowTitleMatchesText", ex);

                return false;
            }
        }
    }
}