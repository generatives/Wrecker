using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Utilities;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Clunker.Editor
{
    public class EditorMenu : Component, IUpdateable
    {
        public static readonly string Path = "C:\\Clunker\\Constructs\\";
        public string SaveTo = Path;
        public string LoadFrom = "";

        private FilePicker _loadFilePicker;
        private FilePicker _saveFilePicker;

        public EditorMenu()
        {
            _loadFilePicker = new FilePicker("Load", Path, false, new[] { ".vspace" });
            _saveFilePicker = new FilePicker("Save", Path, true);
        }

        public void Update(float time)
        {
            ImGui.Begin("Editor");
            int i = 0;
            var gameObjectCopy = CurrentScene.GameObjects.ToList();
            foreach (var obj in gameObjectCopy)
            {
                var name = obj.Name;
                ImGui.InputText($"Name {i}", ref name, 100);
                obj.Name = name;
                if (ImGui.Button($"Save {i}"))
                {
                    var data = CurrentScene.App.Serializer.Serialize(obj);

                    if (!Directory.Exists(SaveTo)) Directory.CreateDirectory(SaveTo);
                    File.WriteAllBytes(SaveTo + "\\" + obj.Name + ".vspace", data);
                }
                ImGui.SameLine();
                if(ImGui.Button($"Remove {i}"))
                {
                    CurrentScene.RemoveGameObject(obj);
                }
                ImGui.Separator();
                i++;
            }

            _saveFilePicker.Draw(ref SaveTo);

            if(_loadFilePicker.Draw(ref LoadFrom))
            {
                var data = File.ReadAllBytes(LoadFrom);
                var obj = CurrentScene.App.Serializer.Deserialize<GameObject>(data);
                obj.Transform.WorldPosition = GameObject.Transform.WorldPosition;
                CurrentScene.AddGameObject(obj);
            }

            //ImGui.InputText("Load From", ref LoadFrom, 100);
            //if(ImGui.Button("Load"))
            //{
            //    var data = File.ReadAllBytes(Path + LoadFrom + ".vspace");
            //    var obj = CurrentScene.App.Serializer.Deserialize<GameObject>(data);
            //    obj.Name = LoadFrom;
            //    obj.Transform.WorldPosition = GameObject.Transform.WorldPosition;
            //    CurrentScene.AddGameObject(obj);
            //}
        }
    }
}
