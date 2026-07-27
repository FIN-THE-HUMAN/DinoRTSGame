using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace RTSFramework.UI
{
    public class RTSButtonRightClickReceiver : MonoBehaviour, IPointerClickHandler
    {
        public Action OnRightClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke();
            }
        }
    }
}
