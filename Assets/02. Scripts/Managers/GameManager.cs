using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    [SerializeField] private WordEater.Core.WordEater wordeater;
    [SerializeField] private GameObject touchblockPanel;

    //인자가 두개씩 필요한 애들

    [Header("전화 관련")]
    [SerializeField] private RectTransform CallPanel;
    [SerializeField] private RectTransform CallBtn;
    // 전화 오는 연출을 제어할 코루틴 변수
    private Coroutine ringingCoroutine;

    [Header("메세지 관련")]
    [SerializeField] private RectTransform MessagePanel;
    [SerializeField] private RectTransform MessageBtn;

    [Header("도감 관련")]
    [SerializeField] private RectTransform GalleryPanel;
    [SerializeField] private RectTransform GalleryBtn;

    [Header("인벤 관련")]
    [SerializeField] private RectTransform FolderPanel;
    [SerializeField] private RectTransform FolderBtn;

    [Header("설정 관련")]
    [SerializeField] private RectTransform SettingPanel;
    [SerializeField] private RectTransform SettingBtn;

    [Header("게임 오버 연출 (배터리 방전)")]
    [SerializeField] private CanvasGroup gameOverCanvasGroup; // 검은 배경 전체 (알파값 조절용)
    [SerializeField] private Image batteryFillImg;            // 빨간색 배터리 게이지
    [SerializeField] private Image cableIconImg;              // 케이블/번개 아이콘

    [Header("워드이터(히스토리) 관련")]
    [SerializeField] private RectTransform WordEaterPanel;
    [SerializeField] private RectTransform WordEaterBtn;

    public static GameManager Instance;

    void Awake() => Instance = this;
    void Start()
    {
        //시작 브금 출력
        SoundManager.Instance.BGMStart(1);


        //시작 하면 첫 정답 단어 선정
        wordeater.BeginStage(wordeater.ReturnStage(), initial: true);

        // 시작 시 게임오버 패널은 꺼두기
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0;
            gameOverCanvasGroup.gameObject.SetActive(false);
        }
    }

    //type 에 따라 게임이 끝났을 때 행동 변화.
    //type이=1 인 경우 <<< 게임 오버.
    //type이=2 인 경우 <<< 게임 클리어
    public void EndingController(int type)
    {
        // 게임 오버 (배터리 방전 연출)
        if (type == 1)
        {
            // 터치 방지
            touchblockPanel.SetActive(true);

            // 기존 텍스트 알림 대신 방전 연출 코루틴 실행
            StartCoroutine(ProcessGameOverSequence());
        }
        // 게임 클리어
        else if (type == 2)
        {
            touchblockPanel.SetActive(true);
            NoticeManager.Instance.ShowTimed("게임 클리어!", 3f);
            StartCoroutine(RestartWithDelay(3f));
        }
    }

    // 배터리 방전 연출 코루틴
    private IEnumerator ProcessGameOverSequence()
    {
        // 패널 활성화 및 초기화
        gameOverCanvasGroup.gameObject.SetActive(true);
        gameOverCanvasGroup.alpha = 0f;

        // 배터리 빨간색 부분과 케이블 아이콘 초기 상태
        batteryFillImg.color = Color.white;
        cableIconImg.color = Color.white;

        // 화면 페이드 인 (검은 화면이 쓱 나타남)
        gameOverCanvasGroup.DOFade(1f, 0.5f);

        // 연출 시작 (DOTween)

        // 빨간 배터리가 깜빡깜빡 (경고 느낌)
        // SetLoops(-1, LoopType.Yoyo) : 무한 반복하면서 밝아졌다 어두워졌다 함
        batteryFillImg.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);

        // 케이블/번개 아이콘이 나타났다 사라졌다 (충전 필요 알림 느낌)
        // 약간 엇박자로 깜빡이게 설정
        cableIconImg.DOFade(0.3f, 0.8f).SetLoops(-1, LoopType.Yoyo);

        // 연출 보여주는 시간 (예: 3초 동안 멍하니 바라보게 함)
        yield return new WaitForSeconds(3.0f);

        // 트윈 제거 (메모리 누수 방지 및 상태 초기화)
        batteryFillImg.DOKill();
        cableIconImg.DOKill();
        gameOverCanvasGroup.DOKill();

        // 알파값 복구
        Color tempFill = batteryFillImg.color; tempFill.a = 1f; batteryFillImg.color = tempFill;
        Color tempIcon = cableIconImg.color; tempIcon.a = 1f; cableIconImg.color = tempIcon;

        // 패널 끄고 재시작
        gameOverCanvasGroup.gameObject.SetActive(false);
        Restart();
    }

    //일단은 N초뒤 시작이지만, 나중에 애니메이션을 넣으면 애니메이션 쪽에서 restart함수 실행으로 변경
    private IEnumerator RestartWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Restart();
    }

    private void Restart() {
        touchblockPanel.SetActive(false);
        wordeater.BeginStage(wordeater.ReturnStage(), initial: true);
    }



    // ---- 공용 유틸 애니메이션 ----
    private void ShowPanelFromButton(RectTransform panel, RectTransform btn)
    {
        if (panel == null || btn == null) return;

        panel.gameObject.SetActive(true);

        var parent = panel.parent as RectTransform;

        // 버튼(Canvas A)의 위치를 패널 부모(Canvas B)의 로컬좌표로 변환
        Vector2 startLocal = CanvasUtil.ConvertBetweenCanvases(btn, parent);

        // 시작 상태
        panel.anchoredPosition = startLocal;
        panel.localScale = Vector3.zero;

        // 목표: 부모 중앙(앵커/피벗이 Center라면 Vector2.zero)
        Vector2 targetLocal = Vector2.zero;

        // 애니메이션
        panel.DOAnchorPos(targetLocal, 0.3f).SetEase(Ease.OutBack);
        panel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    private void HidePanelToButton(RectTransform panel, RectTransform btn)
    {
        if (panel == null || btn == null) return;

        var parent = panel.parent as RectTransform;
        Vector2 endLocal = CanvasUtil.ConvertBetweenCanvases(btn, parent);

        panel.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        panel.DOAnchorPos(endLocal, 0.2f).SetEase(Ease.InBack)
             .OnComplete(() => panel.gameObject.SetActive(false));
    }

    // ---- 단일 인자 버전 (같은 Canvas에서만 사용 시) ----
    public void ShowPanel(RectTransform panel)
    {
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.zero;
        panel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void HidePanel(RectTransform panel)
    {
        if (panel == null) return;
        panel.DOScale(Vector3.zero, 0.2f)
             .SetEase(Ease.InBack)
             .OnComplete(() => panel.gameObject.SetActive(false));
    }

    // ---- 패널별 쇼/하이드 ----
    public void ShowPanel_Call()
    {
        // 패널 등장 (기존 함수)
        ShowPanelFromButton(CallPanel, CallBtn);

        // 전화 오는 연출 시작 (이미 울리고 있다면 중복 방지)
        if (ringingCoroutine != null) StopCoroutine(ringingCoroutine);
        ringingCoroutine = StartCoroutine(ProcessIncomingCall());
    }

    public void HidePanel_Call()
    {
        // 1. 연출 중단
        if (ringingCoroutine != null)
        {
            StopCoroutine(ringingCoroutine);
            ringingCoroutine = null;
        }

        // 2. 흔들림 때문에 돌아간 회전값/위치값 초기화 (중요!)
        CallPanel.transform.rotation = Quaternion.identity;
        // 만약 위치도 흔들었다면 CallPanel.anchoredPosition 도 보정이 필요할 수 있으나, 
        // HidePanelToButton에서 위치를 덮어쓰므로 회전만 초기화해도 괜찮습니다.

        // 3. 패널 퇴장 (기존 함수)
        HidePanelToButton(CallPanel, CallBtn);
    }

    /// <summary>
    /// 전화 오는 연출
    /// </summary>
    private IEnumerator ProcessIncomingCall()
    {
        // 패널이 팝업되는 시간(0.3초)만큼 살짝 대기했다가 진동 시작 (선택사항)
        yield return new WaitForSeconds(0.2f);

        while (true)
        {
            // 기기 진동 (모바일 기기에서만 작동)
            // 기본적으로 0.5~1초 정도 진동합니다.
            Handheld.Vibrate();

            // DOTween을 이용한 시각적 흔들림
            // duration: 0.5초 동안, strength: 30도 강도로, vibrato: 10만큼, randomness: 작을수록 덜 흔들림
            // mode: Rotate (회전하면서 흔들림 - 아이콘이 딸랑거리는 느낌)
            CallPanel.DOShakeRotation(0.5f, 30f, 10, 10, true);

            // 다음 진동까지 대기 (진동 간격)
            // 1초 쉬고 다시 울림 (따르릉~ ... 따르릉~ 느낌)
            yield return new WaitForSeconds(1.2f);
        }
    }

    /// <summary>
    /// 전화 연출 멈추기
    /// </summary>
    public void StopRingingEffect()
    {
        if (ringingCoroutine != null)
        {
            StopCoroutine(ringingCoroutine);
            ringingCoroutine = null;
        }

        // 흔들림으로 인해 틀어진 회전값 원상복구
        if (CallPanel != null)
        {
            CallPanel.transform.rotation = Quaternion.identity;
            // 만약 DOShake 애니메이션이 실행 중이라면 강제로 멈춥니다 (선택사항, 더 확실함)
            CallPanel.DOKill();
        }
    }

    public void ShowPanel_Message() => ShowPanelFromButton(MessagePanel, MessageBtn);
    public void HidePanel_Message() => HidePanelToButton(MessagePanel, MessageBtn);

    public void ShowPanel_Gallery() => ShowPanelFromButton(GalleryPanel, GalleryBtn);
    public void HidePanel_Gallery() => HidePanelToButton(GalleryPanel, GalleryBtn);

    // 다른 Canvas여도 정확히 버튼 자리에서 시작/복귀
    public void ShowPanel_Folder() => ShowPanelFromButton(FolderPanel, FolderBtn);
    public void HidePanel_Folder() => HidePanelToButton(FolderPanel, FolderBtn);

    public void ShowPanel_Setting() => ShowPanelFromButton(SettingPanel, SettingBtn);
    public void HidePanel_Setting() => HidePanelToButton(SettingPanel, SettingBtn);

    public void ShowPanel_WordEater() => ShowPanelFromButton(WordEaterPanel, WordEaterBtn);
    public void HidePanel_WordEater() => HidePanelToButton(WordEaterPanel, WordEaterBtn);
}

/// <summary>
/// Canvas A의 RectTransform 위치를 Canvas B(정확히는 대상 부모 RectTransform)의 로컬좌표로 변환
/// </summary>
public static class CanvasUtil
{
    public static Vector2 ConvertBetweenCanvases(RectTransform fromRT, RectTransform toParent)
    {
        if (fromRT == null || toParent == null) return Vector2.zero;

        var fromCanvas = fromRT.GetComponentInParent<Canvas>();
        var toCanvas = toParent.GetComponentInParent<Canvas>();

        Camera fromCam = (fromCanvas != null && fromCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : fromCanvas != null ? fromCanvas.worldCamera : null;

        Camera toCam = (toCanvas != null && toCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : toCanvas != null ? toCanvas.worldCamera : null;

        // fromRT의 월드 위치를 스크린 좌표로
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(fromCam, fromRT.position);

        // 스크린 좌표를 toParent 로컬좌표로
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            toParent, screenPos, toCam, out var localPoint);

        return localPoint;
    }
}
