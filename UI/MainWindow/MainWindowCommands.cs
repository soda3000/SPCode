﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SPCode.UI.Components;
using SPCode.UI.Windows;
using SPCode.Utils;
using SPCode.Utils.SPSyntaxTidy;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        /// <summary>
        /// Gets the current editor element.
        /// </summary>
        /// <returns></returns>
        public EditorElement GetCurrentEditorElement()
        {
            if (Application.Current != null)
            {
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is NewFileWindow newFileWin)
                    {
                        return newFileWin.PreviewBox;
                    }
                }
            }

            if (DockingPane.SelectedContent?.Content != null)
            {
                var possElement = DockingManager.ActiveContent;
                if (possElement is EditorElement element)
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the current DASM element.
        /// </summary>
        /// <returns></returns>
        public DASMElement GetCurrentDASMElement()
        {
            DASMElement outElement = null;
            if (DockingPane.SelectedContent?.Content != null)
            {
                var possElement = DockingManager.ActiveContent;
                if (possElement is DASMElement element)
                {
                    outElement = element;
                }
            }

            return outElement;
        }

        /// <summary>
        /// Creates a new SourcePawn Script file and loads it.
        /// </summary>
        private void Command_New()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && ee.IsTemplateEditor)
                {
                    return;
                }
                string newFilePath;
                string rootPath;
                var newFileNum = 0;

                if (Program.Configs[Program.SelectedConfig].SMDirectories.Count > 0)
                {
                    rootPath = Program.Configs[Program.SelectedConfig].SMDirectories[0];
                }
                else
                {
                    rootPath = Environment.CurrentDirectory;
                }

                do
                {
                    newFilePath = Path.Combine(rootPath, $"New Plugin ({++newFileNum}).sp");
                } while (File.Exists(newFilePath));

                File.Create(newFilePath).Close();

                AddEditorElement(new FileInfo(newFilePath), $"New Plugin ({newFileNum}).sp", true, out _);
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }

        }

        /// <summary>
        /// Opens a new window for the user to create a new SourcePawn Script from a template.
        /// </summary>
        private void Command_NewFromTemplate()
        {
            try
            {
                var nfWindow = new NewFileWindow
                {
                    Owner = this,
                    ShowInTaskbar = false
                };
                DimmMainWindow();
                nfWindow.ShowDialog();
                RestoreMainWindow();
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }

        }

        /// <summary>
        /// Opens the Open File window for the user to load a file into the editor.
        /// </summary>
        private void Command_Open()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && ee.IsTemplateEditor)
                {
                    return;
                }
                var ofd = new OpenFileDialog
                {
                    AddExtension = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = Constants.FileOpenFilters,
                    Multiselect = true,
                    Title = Translate("OpenNewFile")
                };
                var result = ofd.ShowDialog(this);
                if (result.Value)
                {
                    var AnyFileLoaded = false;
                    if (ofd.FileNames.Length > 0)
                    {
                        for (var i = 0; i < ofd.FileNames.Length; ++i)
                        {
                            AnyFileLoaded |= TryLoadSourceFile(ofd.FileNames[i], out _, i == 0, true, i == 0);
                        }

                        if (!AnyFileLoaded)
                        {
                            MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Theme;
                            this.ShowMessageAsync(Translate("NoFileOpened"),
                                Translate("NoFileOpenedCap"), MessageDialogStyle.Affirmative,
                                MetroDialogOptions);
                        }
                    }
                }

                Activate();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Saves the current file.
        /// </summary>
        private void Command_Save()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.IsTemplateEditor)
                {
                    ee.Save(true);
                    BlendOverEffect.Begin();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Save As window.
        /// </summary>
        private void Command_SaveAs()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.IsTemplateEditor)
                {
                    var sfd = new SaveFileDialog { AddExtension = true, Filter = Constants.FileSaveFilters, OverwritePrompt = true, Title = Translate("SaveFileAs"), FileName = ee.Parent.Title.Trim('*') };
                    var result = sfd.ShowDialog(this);
                    if (result.Value && !string.IsNullOrWhiteSpace(sfd.FileName))
                    {
                        ee.FullFilePath = sfd.FileName;
                        ee.Save(true);
                        BlendOverEffect.Begin();
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Go To Line window.
        /// </summary>
        private void Command_GoToLine()
        {
            try
            {
                if (EditorReferences.Any())
                {
                    var goToLineWindow = new GoToLineWindow();
                    goToLineWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Deletes the line the caret is in.
        /// </summary>
        private void Command_DeleteLine()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.editor.IsReadOnly)
                {
                    ee.DeleteLine();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Search window.
        /// </summary>
        private void Command_FindReplace()
        {
            try
            {
                if (!EditorReferences.Any())
                {
                    return;
                }

                var selection = GetCurrentEditorElement().editor.TextArea.Selection.GetText();

                foreach (Window win in Application.Current.Windows)
                {
                    if (win is SearchWindow findWin)
                    {
                        findWin.Activate();
                        findWin.FindBox.Text = selection;
                        findWin.FindBox.SelectAll();
                        findWin.FindBox.Focus();
                        return;
                    }
                }
                var findWindow = new SearchWindow(selection) { Owner = this };
                findWindow.Show();
                findWindow.FindBox.Focus();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Saves all opened files.
        /// </summary>
        private void Command_SaveAll()
        {
            try
            {
                if (!EditorReferences.Any()|| GetCurrentEditorElement().IsTemplateEditor)
                {
                    return;
                }

                if (EditorReferences.Count > 0)
                {
                    foreach (var editor in EditorReferences)
                    {
                        editor.Save();
                    }

                    BlendOverEffect.Begin();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Closes the current file opened.
        /// </summary>
        private void Command_Close()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                var de = GetCurrentDASMElement();
                if (ee != null && (ee.IsTemplateEditor || ee.ClosingPromptOpened))
                {
                    return;
                }
                ee?.Close();
                de?.Close();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Closes all files opened.
        /// </summary>
        private async void Command_CloseAll()
        {
            try
            {
                // We create a new list because we can't delete elements of the list we're iterating through
                foreach (var editor in EditorReferences.ToList()) editor.Close();
                foreach (var editor in DASMReferences.ToList()) editor.Close();
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Undoes the previous action.
        /// </summary>
        private void Command_Undo()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null)
                {
                    if (ee.editor.CanUndo)
                    {
                        ee.editor.Undo();
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Redoes the latter action.
        /// </summary>
        private void Command_Redo()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null)
                {
                    if (ee.editor.CanRedo)
                    {
                        ee.editor.Redo();
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Cut command.
        /// </summary>
        private void Command_Cut()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.Cut();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Copy command.
        /// </summary>
        private void Command_Copy()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.Copy();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Paste command.
        /// </summary>
        private void Command_Paste()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.Paste();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Collapses or expands all foldings in the editor.
        /// </summary>
        /// <param name="folded">Whether to fold all foldings (true to collapse all).</param>
        private void Command_FlushFoldingState(bool folded)
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee?.foldingManager != null)
                {
                    var foldings = ee.foldingManager.AllFoldings;
                    foreach (var folding in foldings)
                    {
                        folding.IsFolded = folded;
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Select all command.
        /// </summary>
        private void Command_SelectAll()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.SelectAll();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Comment or uncomment the line the caret is in.
        /// </summary>
        private void Command_ToggleCommentLine(bool comment)
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.editor.IsReadOnly)
                {
                    ee.ToggleComment(comment);
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Change the case of the current selection.
        /// </summary>
        /// <param name="toUpper">Whether to transform to uppercase</param>
        public void Command_ChangeCase(bool toUpper)
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.editor.IsReadOnly)
                {
                    ee.ChangeCase(toUpper);
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Perform a code reformat to clean loose whitespaces/wrongly indented code.
        /// </summary>
        /// <param name="all"></param>
        private void Command_TidyCode(bool all)
        {
            try
            {
                var editors = all ? EditorReferences : new() { GetCurrentEditorElement() };
                foreach (var editor in editors)
                {
                    if (editor == null || editor.editor.IsReadOnly)
                    {
                        continue;
                    }

                    int currentCaret = editor.editor.TextArea.Caret.Offset, numOfSpacesOrTabsBefore = 0;
                    var line = editor.editor.Document.GetLineByOffset(currentCaret);
                    var lineNumber = line.LineNumber;
                    // 0 - start | any other - middle | -1 - EOS
                    var cursorLinePos = currentCaret == line.Offset ? 0 : currentCaret == line.EndOffset ? -1 : currentCaret - line.Offset;

                    if (cursorLinePos > 0)
                    {
                        numOfSpacesOrTabsBefore = editor.editor.Document.GetText(line).Count(c => c == ' ' || c == '\t');
                    }

                    // Formatting Start //
                    editor.editor.Document.BeginUpdate();
                    var source = editor.editor.Text;
                    editor.editor.Document.Replace(0, source.Length, SPSyntaxTidy.TidyUp(source));
                    editor.editor.Document.EndUpdate();
                    // Formatting End //

                    line = editor.editor.Document.GetLineByNumber(lineNumber);
                    var newCaretPos = line.Offset;
                    if (cursorLinePos == -1)
                    {
                        newCaretPos += line.Length;
                    }
                    else if (cursorLinePos != 0)
                    {
                        var numOfSpacesOrTabsAfter = editor.editor.Document.GetText(line).Count(c => c == ' ' || c == '\t');
                        newCaretPos += cursorLinePos + (numOfSpacesOrTabsAfter - numOfSpacesOrTabsBefore);
                    }
                    editor.editor.TextArea.Caret.Offset = newCaretPos;
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Open File window for the user to select a file to decompile.
        /// </summary>
        private async void Command_Decompile()
        {
            try
            {
                var file = DecompileUtil.GetFile();
                if (file != null)
                {
                    var msg = await this.ShowProgressAsync(Translate("Decompiling") + "...", file.Name, false, MetroDialogOptions);
                    msg.SetIndeterminate();
                    ProcessUITasks();
                    TryLoadSourceFile(DecompileUtil.GetDecompiledPlugin(file), out _, SelectMe: true);
                    await msg.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Search Definition window.
        /// </summary>
        private void Command_OpenSPDef()
        {
            try
            {
                // Get the currently highlighted text
                var selection = GetCurrentEditorElement().editor.TextArea.Selection.GetText();
                // Create the Search Definition window
                var spDefinitionWindow = new SPDefinitionWindow
                {
                    Owner = this,
                    ShowInTaskbar = false            
                };

                // Set the search box text if there is a selection
                if (!string.IsNullOrEmpty(selection)) 
                {
                    spDefinitionWindow.SPSearchBox.Text = selection;
                }

                DimmMainWindow();
                spDefinitionWindow.ShowDialog();
                RestoreMainWindow();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }
        /// <summary>
        /// Re-opens the last closed tab
        /// </summary>
        private void Command_ReopenLastClosedTab()
        {
            try
            {
                if (Program.RecentFilesStack.Count > 0)
                {
                    TryLoadSourceFile(Program.RecentFilesStack.Pop(), out _, true, false, true);
                }

                MenuI_ReopenLastClosedTab.IsEnabled = Program.RecentFilesStack.Count > 0;
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }
    }
}