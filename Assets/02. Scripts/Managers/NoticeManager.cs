using UnityEngine;

public class NoticeManager : MonoBehaviour
{
    public static NoticeManager Instance;
    public NoticeUI popupPrefab;
    NoticeUI cached;

    void Awake() => Instance = this;

    public void Show(string msg, float seconds = 2f)
    {
        if (cached == null) cached = Instantiate(popupPrefab, gameObject.transform);
        cached.Show(msg, seconds);
    }
}
