using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Veldrid;
using System.Linq;

// Based on https://github.com/mellinoe/synthapp/blob/master/src/synthapp/Widgets/FilePicker.cs#L58

namespace Clunker.Editor
{
    public class FilePicker
    {
        private const string FilePickerID = "###FilePicker";
        private static readonly Vector2 DefaultFilePickerSize = new Vector2(600, 400);

        public string CurrentFolder { get; set; }
        public string SelectedFile { get; set; }

        public string[] Extensions { get; set; }

        public FilePicker(string currentFolder, string[] extensions = null)
        {
            CurrentFolder = currentFolder;
            Extensions = extensions;
        }

        public bool Draw(ref string selected)
        {
            string label = null;
            if (selected != null)
            {
                try
                {
                    var info = new FileInfo(selected);
                    label = info.Name;
                }
                catch (Exception)
                {
                    label = "<Select File>";
                }
            }
            if (ImGui.Button(label))
            {
                ImGui.OpenPopup(FilePickerID);
            }

            bool result = false;
            bool p_open = true;
            if (ImGui.BeginPopupModal(FilePickerID, ref p_open, ImGuiWindowFlags.NoTitleBar))
            {
                result = DrawFolder(ref selected, true);
                ImGui.EndPopup();
            }

            return result;
        }

        private bool DrawFolder(ref string selected, bool returnOnSelection = false)
        {
            ImGui.Text("Current Folder: " + CurrentFolder);
            bool result = false;

            if (ImGui.BeginChildFrame(1, DefaultFilePickerSize, ImGuiWindowFlags.ChildMenu))
            {
                DirectoryInfo di = new DirectoryInfo(CurrentFolder);
                if (di.Exists)
                {
                    if (di.Parent != null)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, RgbaFloat.Yellow.ToVector4());
                        if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
                        {
                            CurrentFolder = di.Parent.FullName;
                        }
                        ImGui.PopStyleColor();
                    }
                    foreach (var fse in Directory.EnumerateFileSystemEntries(di.FullName))
                    {
                        if (Directory.Exists(fse))
                        {
                            string name = Path.GetFileName(fse);
                            ImGui.PushStyleColor(ImGuiCol.Text, RgbaFloat.Yellow.ToVector4());
                            if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
                            {
                                CurrentFolder = fse;
                            }
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            var ext = Path.GetExtension(fse);
                            if(Extensions == null || Extensions.Contains(ext))
                            {
                                var name = Path.GetFileName(fse);
                                bool isSelected = SelectedFile == fse;
                                if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
                                {
                                    SelectedFile = fse;
                                    if (returnOnSelection)
                                    {
                                        result = true;
                                        selected = SelectedFile;
                                    }
                                }
                                if (ImGui.IsMouseDoubleClicked(0))
                                {
                                    result = true;
                                    selected = SelectedFile;
                                    ImGui.CloseCurrentPopup();
                                }
                            }
                        }
                    }
                }

            }
            ImGui.EndChildFrame();


            if (ImGui.Button("Cancel"))
            {
                result = false;
                ImGui.CloseCurrentPopup();
            }

            if (SelectedFile != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Open"))
                {
                    result = true;
                    selected = SelectedFile;
                    ImGui.CloseCurrentPopup();
                }
            }

            return result;
        }
    }
}