using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Utilities;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Editor
{
    public class EditorMenu : Component, IUpdateable
    {
        public string Path = AppContext.BaseDirectory;
        public string LoadFrom = "";

        public void Update(float time)
        {
            ImGui.Begin("Editor");
            int i = 0;
            foreach (var obj in CurrentScene.GameObjects)
            {
                var name = obj.Name;
                ImGui.InputText($"Name {i}", ref name, 100);
                obj.Name = name;
                if (ImGui.Button($"Save {i}"))
                {
                    var data = CurrentScene.App.Serializer.Serialize(obj);
                    File.WriteAllBytes(Path + obj.Name + ".vspace", data);
                    LoadFrom = obj.Name;
                }
                ImGui.Separator();
                i++;
            }

            ImGui.InputText("Load From", ref LoadFrom, 100);
            if(ImGui.Button("Load"))
            {
                var data = File.ReadAllBytes(Path + LoadFrom + ".vspace");
                var obj = CurrentScene.App.Serializer.Deserialize<GameObject>(data);
                obj.Transform.WorldPosition = GameObject.Transform.WorldPosition;
                CurrentScene.AddGameObject(obj);
            }
        }
    }
}
