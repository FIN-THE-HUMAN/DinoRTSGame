using UnityEngine;

namespace RTSFramework.Selection
{
    public interface ISelectable
    {
        Transform Transform { get; }
        GameObject GameObject { get; }
        bool IsPlayerOwned { get; }
        void Select();
        void Deselect();
    }
}
