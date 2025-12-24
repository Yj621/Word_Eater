using UnityEngine;
using UnityEngine.EventSystems;

public class TabDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PhoneSwiper swiper;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (swiper) swiper.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (swiper) swiper.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (swiper) swiper.OnEndDrag(eventData);
    }
}
