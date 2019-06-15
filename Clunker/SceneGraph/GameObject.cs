using Clunker.Graphics;
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

        private Dictionary<Type, Component> _components;
        private List<IUpdateable> _updateables;
        private List<IRenderUpdateable> _renderUpdateables;
        private List<IComponentEventListener> _componentListeners;

        private List<IComponentEventListener> _listenersToStop;

        public GameObject()
        {
            _components = new Dictionary<Type, Component>();
            _updateables = new List<IUpdateable>();
            _renderUpdateables = new List<IRenderUpdateable>();
            _componentListeners = new List<IComponentEventListener>();
            _listenersToStop = new List<IComponentEventListener>();
            AddComponent(new Transform());
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

            if (component is IRenderUpdateable renderUpdateable)
            {
                _renderUpdateables.Add(renderUpdateable);
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

            if (component is IUpdateable updateable)
            {
                _updateables.Remove(updateable);
            }

            if (component is IRenderUpdateable renderUpdateable)
            {
                _renderUpdateables.Remove(renderUpdateable);
            }

            if (component is IComponentEventListener componentListener)
            {
                _componentListeners.Remove(componentListener);
                if (CurrentScene != null && CurrentScene.IsRunning)
                {
                    TryTellComponentStop(componentListener);
                }
            }

            component.IsAlive = false;
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

        public bool HasComponent(Type type)
        {
            return _components.ContainsKey(type);
        }

        internal void AddedToScene(Scene scene)
        {
            CurrentScene = scene;
            if(scene.IsRunning)
            {
                _componentListeners.ForEach(l => l.ComponentStarted());
                foreach (var component in _components.Values)
                {
                    component.IsAlive = true;
                }
            }
        }

        internal void RemovedFromCurrentScene()
        {
            if(CurrentScene.IsRunning)
            {
                _componentListeners.ForEach(l => TryTellComponentStop(l));
                foreach (var component in _components.Values)
                {
                    component.IsAlive = false;
                }
            }
            CurrentScene = null;
        }

        internal void SceneStarted()
        {
            _componentListeners.ForEach(l => l.ComponentStarted());
            foreach (var component in _components.Values)
            {
                component.IsAlive = true;
            }
        }

        internal void SceneStopped()
        {
            _componentListeners.ForEach(l => TryTellComponentStop(l));
            foreach (var component in _components.Values)
            {
                component.IsAlive = false;
            }
        }

        private void TryTellComponentStop(IComponentEventListener listener)
        {
            if (!(listener as Component).HasJobs)
            {
                listener.ComponentStopped();
            }
            else
            {
                _listenersToStop.Add(listener);
            }
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

        internal void RenderUpdate(float time)
        {
            for (int i = 0; i < _renderUpdateables.Count; i++)
            {
                _renderUpdateables[i].RenderUpdate(time);
            }
        }
    }
}
