using Clunker.Graphics;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.SceneGraph
{
    public partial class GameObject
    {
        public string Name { get; set; }

        public Scene CurrentScene { get; private set; }

        internal bool HasJobs => _components.Any(c => c.Value.HasJobs) || _listenersToStop.Any(c => (c as Component).HasJobs);

        private Dictionary<Type, Component> _components;
        private List<IUpdateable> _updateables;
        private List<IComponentEventListener> _componentListeners;

        private List<IComponentEventListener> _listenersToStop;

        public GameObject Parent { get; internal set; }
        private List<GameObject> _children;

        public bool IsActive { get; set; } = true;

        public GameObject()
        {
            _components = new Dictionary<Type, Component>();
            _updateables = new List<IUpdateable>();
            _componentListeners = new List<IComponentEventListener>();
            _listenersToStop = new List<IComponentEventListener>();
            AddComponent(new Transform());

            _children = new List<GameObject>();
        }

        public GameObject(string name) : this()
        {
            Name = name;
        }

        public void AddComponent(Component component)
        {
            if (component.GameObject == this) return;
            if (_components.ContainsKey(component.GetType())) return;

            if (component.GameObject != null) component.GameObject.RemoveComponent(component);

            component.GameObject = this;
            _components[component.GetType()] = component;
            component.IsAlive = true;

            if(component is IUpdateable updateable)
            {
                _updateables.Add(updateable);
            }

            if (component is IRenderable renderable)
            {
                if (CurrentScene != null && CurrentScene.IsRunning)
                {
                    CurrentScene.App.AddRenderable(renderable);
                }
            }

            if (component is IComponentEventListener componentListener)
            {
                _componentListeners.Add(componentListener);
                if (CurrentScene != null && CurrentScene.IsRunning)
                {
                    componentListener.ComponentStarted();
                }
            }
        }

        public void RemoveComponent(Component component)
        {
            if (component.GameObject != this) return;
            component.IsAlive = false;

            if (component is IUpdateable updateable)
            {
                _updateables.Remove(updateable);
            }

            if (component is IRenderable renderable)
            {
                if (CurrentScene != null && CurrentScene.IsRunning)
                {
                    CurrentScene.App.RemoveRenderable(renderable);
                }
            }

            if (component is IComponentEventListener componentListener)
            {
                _componentListeners.Remove(componentListener);
                if (CurrentScene != null && CurrentScene.IsRunning)
                {
                    TryTellComponentStop(componentListener);
                }
            }

            component.GameObject = null;
            _components.Remove(component.GetType());
        }

        public object GetComponent(Type type)
        {
            if(_components.ContainsKey(type))
            {
                return _components[type];
            }
            else
            {
                return null;
            }
        }

        public void AddChild(GameObject gameObject)
        {
            if(gameObject.CurrentScene != null)
            {
                throw new Exception("Tried adding a GameObject which already had a scene");
            }
            if (gameObject.Parent != null)
            {
                throw new Exception("Tried adding a GameObject which already had a parent");
            }
            _children.Add(gameObject);
            gameObject.Parent = this;
            if(CurrentScene != null) gameObject.AddedToScene(CurrentScene);
        }

        public void RemoveChild(GameObject gameObject)
        {
            _children.Remove(gameObject);
            gameObject.Parent = null;
            if (gameObject.CurrentScene != null) gameObject.RemovedFromCurrentScene();
        }

        public bool HasComponent(Type type)
        {
            return _components.ContainsKey(type);
        }

        internal void AddedToScene(Scene scene)
        {
            CurrentScene = scene;
            foreach(var gameObject in _children)
            {
                gameObject.AddedToScene(CurrentScene);
            }
            if(scene.IsRunning)
            {
                foreach (var component in _components.Values)
                {
                    component.IsAlive = true;
                }
                _componentListeners.ForEach(l => l.ComponentStarted());
                foreach(var renderable in _components.Values.OfType<IRenderable>())
                {
                    CurrentScene.App.AddRenderable(renderable);
                }
            }
        }

        internal void RemovedFromCurrentScene()
        {
            if(CurrentScene.IsRunning)
            {
                foreach (var component in _components.Values)
                {
                    component.IsAlive = false;
                }
                _componentListeners.ForEach(l => TryTellComponentStop(l));
                foreach (var renderable in _components.Values.OfType<IRenderable>())
                {
                    CurrentScene.App.RemoveRenderable(renderable);
                }
            }
            foreach (var gameObject in _children)
            {
                gameObject.RemovedFromCurrentScene();
            }
            CurrentScene = null;
        }

        internal void SceneStarted()
        {
            foreach (var component in _components.Values)
            {
                component.IsAlive = true;
            }
            _componentListeners.ForEach(l => l.ComponentStarted());
            foreach (var renderable in _components.Values.OfType<IRenderable>())
            {
                CurrentScene.App.AddRenderable(renderable);
            }
            foreach (var gameObject in _children)
            {
                gameObject.SceneStarted();
            }
        }

        internal void SceneStopped()
        {
            foreach (var component in _components.Values)
            {
                component.IsAlive = false;
            }
            _componentListeners.ForEach(l => TryTellComponentStop(l));
            foreach (var renderable in _components.Values.OfType<IRenderable>())
            {
                CurrentScene.App.RemoveRenderable(renderable);
            }
            foreach (var gameObject in _children)
            {
                gameObject.SceneStopped();
            }
        }

        private void TryTellComponentStop(IComponentEventListener listener)
        {
            listener.ComponentStopped();
            //if (!(listener as Component).HasJobs)
            //{
            //    listener.ComponentStopped();
            //}
            //else
            //{
            //    _listenersToStop.Add(listener);
            //}
        }

        internal void Update(float time)
        {
            for(int i = 0; i < _updateables.Count; i++)
            {
                if(_updateables[i].IsActive) _updateables[i].Update(time);
            }

            List<IComponentEventListener> newList = new List<IComponentEventListener>();
            for (int i = 0; i < _listenersToStop.Count; i++)
            {
                var listener = _listenersToStop[i];
                if(!(listener as Component).HasJobs)
                {
                    listener.ComponentStopped();
                }
                else
                {
                    newList.Add(listener);
                }
            }
            _listenersToStop = newList;

            foreach (var gameObject in _children)
            {
                gameObject.Update(time);
            }
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
