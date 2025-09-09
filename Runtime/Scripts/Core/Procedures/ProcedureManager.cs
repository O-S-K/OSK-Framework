using System;
using UnityEngine;

namespace OSK
{
    public class ProcedureManager : GameFrameworkComponent 
    {
        private ProcedureProcessor procedureProcessor;
        private Type procedureNodeType = typeof(ProcedureNode);
        private Action<ProcedureNodeAddedEventArgs> onProcedureNodeAdd;
        private Action<ProcedureNodeRemovedEventArgs> onProcedureNodeRemove;
        private Action<ProcedureNodeChangedEventArgs> onProcedureNodeChange;
        private bool isPause;

        public override void OnInit()
        {
            procedureProcessor = new ProcedureProcessor();
            procedureProcessor.OnProcedureNodeAdd += ProcedureNodeAddCallback;
            procedureProcessor.OnProcedureNodeRemove += ProcedureNodeRemoveCallback;
            procedureProcessor.OnProcedureNodeChange += ProcedureNodeChangedCallback;
            procedureProcessor.FindAllProcedureNode();
        }
        
        /// <summary>
        /// Gets the total number of procedure nodes.
        /// </summary>
        public int ProcedureNodeCount => procedureProcessor.NodeCount;

        /// <summary>
        /// Gets the current procedure node.
        /// </summary>
        public ProcedureNode CurrentProcedureNode => procedureProcessor.CurrentNode;

        /// <summary>
        /// Event triggered when a procedure node is added.
        /// </summary>
        public event Action<ProcedureNodeAddedEventArgs> OnProcedureNodeAdd
        {
            add => onProcedureNodeAdd += value;
            remove => onProcedureNodeAdd -= value;
        }

        /// <summary>
        /// Event triggered when a procedure node is removed.
        /// </summary>
        public event Action<ProcedureNodeRemovedEventArgs> OnProcedureNodeRemove
        {
            add => onProcedureNodeRemove += value;
            remove => onProcedureNodeRemove -= value;
        }

        /// <summary>
        /// Event triggered when the procedure node changes.
        /// </summary>
        public event Action<ProcedureNodeChangedEventArgs> OnProcedureNodeChange
        {
            add => onProcedureNodeChange += value;
            remove => onProcedureNodeChange -= value;
        }

        /// <summary>
        /// Adds procedure nodes.
        /// </summary>
        public void AddProcedureNodes(params ProcedureNode[] nodes)
        {
            procedureProcessor.AddNodes(nodes);
        }

        /// <summary>
        /// Runs a procedure node by type.
        /// </summary>
        public void RunProcedureNode<T>() where T : ProcedureNode
        {
            RunProcedureNode(typeof(T));
        }

        /// <summary>
        /// Runs a procedure node by type.
        /// </summary>
        public void RunProcedureNode(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("Invalid type!");
            if (!procedureNodeType.IsAssignableFrom(type))
                throw new NotImplementedException($"Type: {type} is not derived from ProcedureNode.");
            procedureProcessor.ChangeNode(type);
        }

        /// <summary>
        /// Checks if a procedure node of the specified type exists.
        /// </summary>
        public bool HasProcedureNode<T>() where T : ProcedureNode
        {
            return HasProcedureNode(typeof(T));
        }

        /// <summary>
        /// Checks if a procedure node of the specified type exists.
        /// </summary>
        public bool HasProcedureNode(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("Invalid type!");
            if (!procedureNodeType.IsAssignableFrom(type))
                throw new NotImplementedException($"Type: {type} is not derived from ProcedureNode.");
            return procedureProcessor.HasNode(type);
        }

        /// <summary>
        /// Peeks a procedure node of the specified type.
        /// </summary>
        public bool PeekProcedureNode(Type type, out ProcedureNode node)
        {
            if (type == null)
                throw new ArgumentNullException("Invalid type!");
            if (!procedureNodeType.IsAssignableFrom(type))
                throw new NotImplementedException($"Type: {type} is not derived from ProcedureNode.");
            node = null;
            return procedureProcessor.PeekNode(type, out node);
        }

        /// <summary>
        /// Peeks a procedure node of the specified type.
        /// </summary>
        public bool PeekProcedureNode<T>(out T node) where T : ProcedureNode
        {
            node = default;
            var type = typeof(T);
            if (PeekProcedureNode(type, out var procedureNode))
            {
                node = (T)procedureNode;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes procedure nodes by types.
        /// </summary>
        public void RemoveProcedureNodes(params Type[] types)
        {
            foreach (var type in types)
            {
                RemoveProcedureNode(type);
            }
        }

        /// <summary>
        /// Removes a procedure node of the specified type.
        /// </summary>
        public bool RemoveProcedureNode<T>() where T : ProcedureNode
        {
            return RemoveProcedureNode(typeof(T));
        }

        /// <summary>
        /// Removes a procedure node by type.
        /// </summary>
        public bool RemoveProcedureNode(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("Invalid type!");
            if (!procedureNodeType.IsAssignableFrom(type))
                throw new NotImplementedException($"Type: {type} is not derived from ProcedureNode.");
            return procedureProcessor.RemoveNode(type);
        }
 
        public void SetPause(bool pause)
        {
            isPause = pause;
        }
        
        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
        }

        public void OnUpdate()
        {
            if (isPause)
                return;
            procedureProcessor.OnUpdate();
        }

        public void OnLateUpdate()
        {
            if (isPause)
                return;
            procedureProcessor.OnLateUpdate();
        }

        public void OnFixedUpdate()
        {
            if (isPause)
                return;
            procedureProcessor.OnFixedUpdate();
        }

        public void OnTermination()
        {
            procedureProcessor.ClearAllNode();
            procedureProcessor.OnProcedureNodeAdd -= ProcedureNodeAddCallback;
            procedureProcessor.OnProcedureNodeRemove -= ProcedureNodeRemoveCallback;
            procedureProcessor.OnProcedureNodeChange -= ProcedureNodeChangedCallback;
        }

        private void ProcedureNodeAddCallback(Type type)
        {
            var eventArgs = ProcedureNodeAddedEventArgs.Create(type);
            onProcedureNodeAdd?.Invoke(eventArgs);
            ProcedureNodeAddedEventArgs.Release(eventArgs);
        }

        private void ProcedureNodeRemoveCallback(Type type)
        {
            var eventArgs = ProcedureNodeRemovedEventArgs.Create(type);
            onProcedureNodeRemove?.Invoke(eventArgs);
            ProcedureNodeRemovedEventArgs.Release(eventArgs);
        }

        private void ProcedureNodeChangedCallback(Type exitedNodeType, Type enteredNodeType)
        {
            var eventArgs = ProcedureNodeChangedEventArgs.Create(exitedNodeType, enteredNodeType);
            onProcedureNodeChange?.Invoke(eventArgs);
            ProcedureNodeChangedEventArgs.Release(eventArgs);
        }
    }
}