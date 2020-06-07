using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Veldrid;
using System.Linq;

// Based on https://github.com/mellinoe/synthapp/blob/master/src/synthapp/Widgets/FilePicker.cs#L58

namespace Clunker.Editor.FilePicker
{
    public class FilePicker
    {
        private static readonly Vector2 DefaultFilePickerSize = new Vector2(600, 400);

        public static void Open(string id)
        {
            ImGui.OpenPopup(id);
        }

        public static bool Window(string id, ref string selected, string[] extensions = null)
        {
            bool result = false;
            bool p_open = true;
            if (ImGui.BeginPopupModal(id, ref p_open, ImGuiWindowFlags.NoTitleBar))
            {
                result = DrawFolder(ref selected, extensions);
                ImGui.EndPopup();
            }

            return result;
        }

        private static bool DrawFolder(ref string selected, string[] extensions = null)
        {
            var currentDirectory = Directory.Exists(selected) ? selected : (new FileInfo(selected)).DirectoryName;

            ImGui.Text("Current Folder: " + currentDirectory);

            if (ImGui.BeginChildFrame(1, DefaultFilePickerSize, ImGuiWindowFlags.ChildMenu))
            {
                DirectoryInfo di = new DirectoryInfo(currentDirectory);
                if (di.Exists)
                {
                    if (di.Parent != null)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, RgbaFloat.Yellow.ToVector4());
                        if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
                        {
                            selected = di.Parent.FullName;
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
                                selected = fse;
                            }
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            var ext = Path.GetExtension(fse);
                            if(extensions == null || extensions.Contains(ext))
                            {
                                var name = Path.GetFileName(fse);
                                bool isSelected = selected == fse;
                                if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
                                {
                                    selected = fse;
                                }
                            }
                        }
                    }
                }

            }
            ImGui.EndChildFrame();

            var fileName = Directory.Exists(selected) ? "" : (new FileInfo(selected)).Name;
            ImGui.InputText("Name", ref fileName, 64);
            selected = Directory.Exists(selected) ?
                (new DirectoryInfo(selected)).FullName + "\\" + fileName :
                (new FileInfo(selected)).DirectoryName + "\\" + fileName;

            ImGui.SameLine();
            if (ImGui.Button("Okay"))
            {
                ImGui.CloseCurrentPopup();
                return true;
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
                return false;
            }

            return false;
        }
    }
}