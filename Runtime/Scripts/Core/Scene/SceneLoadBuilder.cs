using System;

namespace OSK
{
    public class SceneLoadBuilder
    {
        private readonly DirectorManager _manager;
        private readonly DataScene[] _sceneNames;
        private bool _async = true;
        private float _fakeDuration = 0f;
        private Action _onStart;
        private Action _onComplete;

        public SceneLoadBuilder(DirectorManager manager, params DataScene[] sceneNames)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            if (sceneNames == null || sceneNames.Length == 0)
                throw new ArgumentException("You must provide at least one scene name.", nameof(sceneNames));

            _sceneNames = sceneNames;
        }

     
        /// <summary>
        /// Set load as async or sync
        /// </summary>
        public SceneLoadBuilder Async(bool async)
        {
            _async = async;
            return this;
        }

        /// <summary>
        /// Fake loading duration (for UI smoothness)
        /// </summary>
        public SceneLoadBuilder FakeDuration(float seconds)
        {
            _fakeDuration = Math.Max(0f, seconds);
            return this;
        }

        /// <summary>
        /// Action before load starts
        /// </summary>
        public SceneLoadBuilder OnStart(Action action)
        {
            _onStart = action;
            return this;
        }

        /// <summary>
        /// Action after load finishes
        /// </summary>
        public SceneLoadBuilder OnComplete(Action action)
        {
            _onComplete = action;
            return this;
        }

        /// <summary>
        /// Execute load with the current builder config
        /// </summary>
        public void Build()
        {
            _manager.StartLoad(
                scenes: _sceneNames,
                async: _async,
                fakeDuration: _fakeDuration,
                onStart: _onStart,
                onComplete: _onComplete
            );
        }
    }
}
