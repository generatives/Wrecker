using Clunker.Graphics;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.ComponentsInterfaces;
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
        public Scene CurrentScene { get; private set; }

        internal bool HasJobs => _components.Any(c => c.Value.HasJobs) || _listenersToStop.Any(c => (c as Component).HasJobs);

        private Dictionary<Type, Component> _components;
        private List<IUpdateable> _updateables;
        private List<IComponentEventListener> _componentListeners;

        private List<IComponentEventListener> _listenersToStop;

        public GameObject Parent { get; internal set; }
        private List<GameObject> _children;

        public GameObject()
        {
            _components = new Dictionary<Type, Component>();
            _updateables = new List<IUpdateable>();
            _componentListeners = new List<IComponentEventListener>();
            _listenersToStop = new List<IComponentEventListener>();
            AddComponent(new Transform());

            _children = new List<GameObject>();
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
            if(gameObject.Parent != null)
            {
                gameObject.Parent.RemoveChild(gameObject);
            }
            _children.Add(gameObject);
            gameObject.Parent = this;
        }

        public void RemoveChild(GameObject gameObject)
        {
            _children.Remove(gameObject);
        }

        public bool HasComponent(Type type)
        {
            return _components.ContainsKey(type);
        }

        internal void AddedToScene(Scene scene)
        {
            CurrentScene = scene;
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
                _updateables[i].Update(time);
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
        }
    }
}
