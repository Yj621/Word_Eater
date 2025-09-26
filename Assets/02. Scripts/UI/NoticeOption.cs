public enum NoticeDismissMode
{
    Auto,     // 지정 시간 뒤 자동 닫힘
    Button,   // X 버튼(또는 닫기 버튼)으로만 닫힘
    Manual    // 외부에서 Dismiss() 호출해야 닫힘
}

public sealed class NoticeOptions
{
    public string Message;
    public NoticeDismissMode DismissMode = NoticeDismissMode.Auto;
    public float Duration = 2f;      // Auto에서만 의미 있음
    public bool TapToDismiss = false; // 탭으로 닫기 허용(원하면)
    public System.Action OnClosed;    // 닫힌 뒤 콜백

    // 편의 생성자들
    public static NoticeOptions Timed(string msg, float seconds = 2f) =>
        new NoticeOptions { Message = msg, DismissMode = NoticeDismissMode.Auto, Duration = seconds };

    public static NoticeOptions Sticky(string msg) =>
        new NoticeOptions { Message = msg, DismissMode = NoticeDismissMode.Button };

    public static NoticeOptions Manual(string msg) =>
        new NoticeOptions { Message = msg, DismissMode = NoticeDismissMode.Manual };
}
