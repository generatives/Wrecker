using Clunker.SceneGraph.ComponentInterfaces;
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
        private List<GameObject> _toRemove;
        public IEnumerable<GameObject> RootGameObjects => _gameObjects;
        public IEnumerable<GameObject> GameObjects => _gameObjects.Concat(_gameObjects.SelectMany(c => c.Descendents));

        private List<IUpdatableSystem> _updatables;
        private List<ISystemEventProcessor> _eventProcessors;
        private Dictionary<Type, SceneSystem> _systems;

        public DrivenWorkQueue FrameQueue { get; private set; }

        public bool IsRunning { get; private set; }
        public Camera Camera => App.Camera;
        public ClunkerApp App { get; private set; }

        public Scene()
        {
            _gameObjects = new List<GameObject>();
            _toRemove = new List<GameObject>();
            _updatables = new List<IUpdatableSystem>();
            _eventProcessors = new List<ISystemEventProcessor>();
            _systems = new Dictionary<Type, SceneSystem>();
            FrameQueue = new DrivenWorkQueue();
        }

        internal void SceneStarted(ClunkerApp app)
        {
            App = app;
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                _gameObjects[i].SceneStarted();
            }
            for (int i = 0; i < _eventProcessors.Count; i++)
            {
                _eventProcessors[i].SystemStarted();
            }
            IsRunning = true;
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
            if(gameObject.CurrentScene == null)
            {
                _gameObjects.Add(gameObject);
                gameObject.AddedToScene(this);
            }
            else
            {
                throw new Exception("Tried adding a GameObject which already had a scene");
            }
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            if(gameObject.CurrentScene == this)
            {
                _gameObjects.Remove(gameObject);
                gameObject.RemovedFromCurrentScene();
                //if (gameObject.HasJobs)
                //{
                //    _toRemove.Add(gameObject);
                //}
                //else
                //{
                //    gameObject.RemovedFromCurrentScene();
                //}
            }
        }

        internal void Update(float time)
        {
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                if(_gameObjects[i].IsActive) _gameObjects[i].Update(time);
            }

            FrameQueue.ConsumeActions();

            for (int i = 0; i < _updatables.Count; i++)
            {
                _updatables[i].Update(time);
            }

            var toRemove = new List<GameObject>(_toRemove.Count);
            for (int i = 0; i < _toRemove.Count; i++)
            {
                var obj = _toRemove[i];
                if(obj.HasJobs)
                {
                    toRemove.Add(obj);
                }
                else
                {
                    obj.RemovedFromCurrentScene();
                }
            }
            _toRemove = toRemove;
        }
    }
}
