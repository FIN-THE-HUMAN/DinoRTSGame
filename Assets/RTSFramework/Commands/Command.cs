using UnityEngine;

namespace RTSFramework.Commands
{
    public abstract class Command
    {
        public bool IsFinished { get; protected set; }
        public abstract void Execute(GameObject unit);
        public abstract void Update(GameObject unit);
        public abstract void Cancel(GameObject unit);
    }
}
