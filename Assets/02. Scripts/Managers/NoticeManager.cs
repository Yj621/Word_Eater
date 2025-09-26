using UnityEngine;

public interface INoticeHandle
{
    void Dismiss();
    bool IsShowing { get; }
}

class NoticeHandle : INoticeHandle
{
    readonly NoticeUI ui;
    public NoticeHandle(NoticeUI ui) => this.ui = ui;
    public void Dismiss() => ui?.Dismiss();
    public bool IsShowing => ui != null && ui.IsShowing;
}

public class NoticeManager : MonoBehaviour
{
    public static NoticeManager Instance;
    public NoticeUI popupPrefab;
    private NoticeUI cached;

    void Awake() => Instance = this;

    NoticeUI Ensure()
    {
        if (cached == null) cached = Instantiate(popupPrefab, transform);
        return cached;
    }

    // 가장 범용
    public INoticeHandle Show(NoticeOptions options)
    {
        var ui = Ensure();
        ui.Show(options);
        return new NoticeHandle(ui);
    }

    // 편의 오버로드들
    public INoticeHandle ShowTimed(string msg, float seconds = 2f) =>
        Show(NoticeOptions.Timed(msg, seconds));

    public INoticeHandle ShowSticky(string msg) =>
        Show(NoticeOptions.Sticky(msg));

    public INoticeHandle ShowManual(string msg) =>
        Show(NoticeOptions.Manual(msg));
}
