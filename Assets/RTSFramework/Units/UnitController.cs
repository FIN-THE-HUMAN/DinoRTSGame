using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Commands;
using RTSFramework.Selection;

namespace RTSFramework.Units
{
    public class UnitController : MonoBehaviour, ISelectable
    {
        [SerializeField] private GameObject selectionVisual;
        [SerializeField] private Factions.Faction faction;
        [SerializeField] private UnitData unitData;

        private readonly Queue<Command> commandQueue = new Queue<Command>();
        private Command currentCommand;

        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public Factions.Faction Faction => faction;
        public UnitData UnitData => unitData;
        public bool IsPlayerOwned => faction != null && faction.IsPlayerFaction;
        public bool HasActiveCommand => currentCommand != null || commandQueue.Count > 0;

        private void OnEnable()
        {
            SelectionManager.RegisterSelectable(this);
        }

        private void OnDisable()
        {
            SelectionManager.UnregisterSelectable(this);
        }

        private void Start()
        {
            Deselect();
        }

        private void Update()
        {
            ProcessCommands();
        }

        public void GiveCommand(Command command, bool queue = false)
        {
            if (!queue)
            {
                CancelAllCommands();
            }
            commandQueue.Enqueue(command);
        }

        private void ProcessCommands()
        {
            if (currentCommand == null && commandQueue.Count > 0)
            {
                currentCommand = commandQueue.Dequeue();
                currentCommand.Execute(gameObject);
            }

            if (currentCommand != null)
            {
                currentCommand.Update(gameObject);
                if (currentCommand.IsFinished)
                {
                    currentCommand = null;
                }
            }
        }

        public void CancelAllCommands()
        {
            currentCommand?.Cancel(gameObject);
            currentCommand = null;
            foreach (var cmd in commandQueue)
            {
                cmd.Cancel(gameObject);
            }
            commandQueue.Clear();
        }

        // ISelectable implementation
        public void Select()
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(true);
            }
        }

        public void Deselect()
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(false);
            }
        }
    }
}
