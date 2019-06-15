﻿using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clunker.SceneGraph;
using Veldrid;
using Veldrid.Sdl2;
using Clunker.SceneGraph.SceneSystemInterfaces;
using System.Collections.Concurrent;
using Clunker.Runtime;

namespace Clunker.SceneGraph
{
    public class Scene
    {
        private List<GameObject> _gameObjects;
        public IEnumerable<GameObject> GameObjects { get => _gameObjects; }

        private List<IUpdatableSystem> _updatables;
        private List<ISystemEventProcessor> _eventProcessors;
        private Dictionary<Type, SceneSystem> _systems;

        public DrivenMetaQueue FrameQueue { get; private set; }

        public bool IsRunning { get; private set; }
        public Camera Camera => App.Camera;
        public ClunkerApp App { get; private set; }

        public Scene()
        {
            _gameObjects = new List<GameObject>();
            _updatables = new List<IUpdatableSystem>();
            _eventProcessors = new List<ISystemEventProcessor>();
            _systems = new Dictionary<Type, SceneSystem>();
            FrameQueue = new DrivenMetaQueue();
        }

        internal void SceneStarted(ClunkerApp app)
        {
            App = app;
            IsRunning = true;
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                _gameObjects[i].SceneStarted();
            }
            for (int i = 0; i < _eventProcessors.Count; i++)
            {
                _eventProcessors[i].SystemStarted();
            }
        }

        internal void SceneStopped()
        {
            App = null;
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                _gameObjects[i].SceneStopped();
            }
            for (int i = 0; i < _eventProcessors.Count; i++)
            {
                _eventProcessors[i].SystemStopped();
            }
        }

        public void AddSystem(SceneSystem system)
        {
            if (_systems.ContainsKey(system.GetType())) return;

            system.CurrentScene = this;
            _systems[system.GetType()] = system;

            if(system is IUpdatableSystem updateable)
            {
                _updatables.Add(updateable);
            }

            if (system is ISystemEventProcessor eventProcessor)
            {
                _eventProcessors.Add(eventProcessor);
                if(IsRunning)
                {
                    eventProcessor.SystemStarted();
                }
            }
        }

        public void RemoveSystem<T>() where T : SceneSystem
        {
            RemoveSystem(typeof(T));
        }

        public void RemoveSystem(Type type)
        {
            if (_systems.Remove(type, out SceneSystem system))
            {
                system.CurrentScene = null;

                if (system is IUpdatableSystem updateable)
                {
                    _updatables.Remove(updateable);
                }

                if (system is ISystemEventProcessor eventProcessor)
                {
                    _eventProcessors.Remove(eventProcessor);
                    if (IsRunning)
                    {
                        eventProcessor.SystemStopped();
                    }
                }
            }
        }

        public T GetSystem<T>() where T : SceneSystem
        {
            if(_systems.ContainsKey(typeof(T)))
            {
                return _systems[typeof(T)] as T;
            }
            else
            {
                return null;
            }
        }

        public T GetOrCreateSystem<T>() where T : SceneSystem, new()
        {
            var system = GetSystem<T>();
            if(system == null)
            {
                system = new T();
                AddSystem(system);
            }
            return system;
        }

        public SceneSystem GetSystem(Type type)
        {
            if (_systems.ContainsKey(type))
            {
                return _systems[type];
            }
            else
            {
                return null;
            }
        }

        public void AddGameObject(GameObject gameObject)
        {
            if(gameObject.CurrentScene != this)
            {
                if (gameObject.CurrentScene != null) gameObject.CurrentScene.RemoveGameObject(gameObject);
                _gameObjects.Add(gameObject);
                gameObject.AddedToScene(this);
                if (IsRunning) gameObject.SceneStarted();
            }
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            if(gameObject.CurrentScene == this)
            {
                _gameObjects.Remove(gameObject);
                gameObject.SceneStopped();
                gameObject.RemovedFromCurrentScene();
            }
        }

        internal void Update(float time)
        {
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                _gameObjects[i].Update(time);
            }

            FrameQueue.ConsumeAllActions();

            for (int i = 0; i < _updatables.Count; i++)
            {
                _updatables[i].Update(time);
            }
        }

        internal void RenderUpdate(float time)
        {
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                _gameObjects[i].RenderUpdate(time);
            }
        }
    }
}