﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.SettingsUI
{
    public static class SettingsUIUtil
    {
        public static void FocusFirstTabStop(this TabControl tabControl)
        {
            Control.ControlCollection controls = tabControl.SelectedTab.Controls;
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                if (controls[i].TabStop)
                {
                    controls[i].Focus();
                    break;
                }
            }            
        }

        public delegate bool ValidateCellValues(string[] values);

        public static bool DoPaste(this DataGridView grid, IWin32Window parent, ValidateCellValues validate)
        {
            string textClip;
            try
            {
                textClip = Clipboard.GetText();
            }
            catch (ExternalException)
            {
                MessageDlg.Show(parent, ClipboardHelper.GetOpenClipboardMessage("Failed getting data from the clipboard."));
                return false;
            }

            if (!grid.EndEdit())
                return false;

            grid.SuspendLayout();

            TextReader reader = new StringReader(textClip);

            // Remove everything, and paste new contents
            grid.Rows.Clear();

            int lineNum = 0;
            String line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                String[] columns = line.Split('\t');
                if (columns.Length > grid.ColumnCount)
                {
                    string message = string.Format("Incorrect number of columns ({0}) found at line {1}.",
                                                   columns.Length, lineNum);
                    MessageDlg.Show(parent, message);
                    break;
                }

                for (int i = 0; i < columns.Length; i++)
                    columns[i] = columns[i].Trim();

                if (!validate(columns))
                    break;

                grid.Rows.Add(columns);
            }

            grid.ResumeLayout();
            return true;
        }

        public static bool DoDelete(this DataGridView grid)
        {
            if (!grid.EndEdit())
                return false;

            grid.SuspendLayout();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (!row.IsNewRow && row.SelectedCells())
                    grid.Rows.Remove(row);
            }

            grid.ResumeLayout();
            return true;
        }

        public static bool SelectedCells(this DataGridViewRow row)
        {
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (!cell.Selected)
                    return false;
            }

            return true;
        }
    }
}
