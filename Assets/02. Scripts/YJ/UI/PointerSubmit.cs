using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class PointerSubmit : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private UnityEvent onSubmit;

    public void OnPointerUp(PointerEventData eventData)
    {
        onSubmit?.Invoke();
    }
}
