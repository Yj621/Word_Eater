using UnityEngine;
using UnityEngine.EventSystems;
public class TouchPanel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [HideInInspector]
    public bool isPressed = false;

    public void OnPointerDown(PointerEventData eventData) => isPressed = true;
    public void OnPointerUp(PointerEventData eventData) => isPressed = false;
    public void OnPointerExit(PointerEventData eventData) => isPressed = false;
}
