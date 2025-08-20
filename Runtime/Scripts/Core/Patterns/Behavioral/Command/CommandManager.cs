using UnityEngine;
using System.Collections.Generic;

namespace OSK
{
    public class CommandManager : GameFrameworkComponent
    {
        [SerializeReference]
        private Dictionary<string, Stack<ICommand>> k_CommandHistory = new Dictionary<string, Stack<ICommand>>();
        public override void OnInit() {}

        public void Create(string commandName, ICommand command)
        {
            if (!k_CommandHistory.ContainsKey(commandName))
            {
                k_CommandHistory[commandName] = new Stack<ICommand>();
            }

            command.Execute();
            k_CommandHistory[commandName].Push(command);
        }

        public void Undo(string commandName)
        {
            if (k_CommandHistory.ContainsKey(commandName) && k_CommandHistory[commandName].Count > 0)
            {
                ICommand command = k_CommandHistory[commandName].Pop();
                command.Undo();
            }
        }
        
        public void UndoAll(string commandName)
        {
            if (k_CommandHistory.ContainsKey(commandName))
            {
                while (k_CommandHistory[commandName].Count > 0)
                {
                    ICommand command = k_CommandHistory[commandName].Pop();
                    command.Undo();
                }
            }
        }
        
        public void Redo(string commandName)
        {
            if (k_CommandHistory.ContainsKey(commandName) && k_CommandHistory[commandName].Count > 0)
            {
                ICommand command = k_CommandHistory[commandName].Peek();
                command.Execute();
                k_CommandHistory[commandName].Push(command);
            }
        }
        
        public void RedoAll(string commandName)
        {
            if (k_CommandHistory.ContainsKey(commandName))
            {
                Stack<ICommand> commands = k_CommandHistory[commandName];
                Stack<ICommand> tempStack = new Stack<ICommand>();

                while (commands.Count > 0)
                {
                    ICommand command = commands.Pop();
                    command.Execute();
                    tempStack.Push(command);
                }

                // Restore the original stack
                while (tempStack.Count > 0)
                {
                    commands.Push(tempStack.Pop());
                }
            }
        }
        
        public Stack<ICommand> GetHistory(string commandName)
        {
            if (k_CommandHistory.ContainsKey(commandName))
            {
                return k_CommandHistory[commandName];
            }
            return null;
        }
         
        
        public void ClearHistory(string commandName)
        {
            if (k_CommandHistory.ContainsKey(commandName))
            {
                k_CommandHistory[commandName].Clear();
            }
        }
        
        public void ClearAllHistory()
        {
            foreach (var commandStack in k_CommandHistory.Values)
            {
                commandStack.Clear();
            }
            k_CommandHistory.Clear();
        }
        
        public bool HasCommand(string commandName)
        {
            return k_CommandHistory.ContainsKey(commandName) && k_CommandHistory[commandName].Count > 0;
        }
    }
}