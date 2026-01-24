using UnityEngine;

public class MiniGameHook : MonoBehaviour
{
    private MiniGameController _controller;

    public void Bind(MiniGameController controller)
    {
        _controller = controller;
    }

    // 미니게임 클리어 시 호출
    public void ReportClear()
    {
        SoundManager.Instance.SFXStart(SoundManager.SFXType.minigameWin);
        _controller?.NotifyClear();
    }

    // 미니게임 실패 시 호출(틀림/충돌/패배 등)
    public void ReportFail()
    {
        SoundManager.Instance.SFXStart(SoundManager.SFXType.minigameLose);
        _controller?.NotifyFail();
    }
}
