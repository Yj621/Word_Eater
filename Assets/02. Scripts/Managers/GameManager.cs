using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
using WordEater.Core;
using WordEater.Systems;

public class GameManager : MonoBehaviour
{
    [SerializeField] private WordEater.Core.WordEater wordeater;
    [SerializeField] private GameObject touchblockPanel;
    public GameObject wordEaterNamePanel;
    [SerializeField] private BatterySystem batterySystem;
    [SerializeField] private FileManager filemanager;
    [SerializeField] private PhoneSwiper phoneSwiper;

    [Header("전화 관련")]
    [SerializeField] private RectTransform CallPanel;
    [SerializeField] private RectTransform CallPanel2;
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
    [SerializeField] private CanvasGroup folderCanvasGroup;

    [Header("설정 관련")]
    [SerializeField] private RectTransform SettingPanel;
    [SerializeField] private RectTransform SettingBtn;

    [Header("아이템 관련")]
    [SerializeField] private RectTransform ItemFolderPanel;
    [SerializeField] private RectTransform ItemFolderBtn;
    [SerializeField] private CanvasGroup itemFolderCanvasGroup;

    [Header("게임 오버 연출 (배터리 방전)")]
    [SerializeField] private CanvasGroup gameOverCanvasGroup; // 검은 배경 전체 (알파값 조절용)
    [SerializeField] private Image batteryFillImg;            // 빨간색 배터리 게이지
    [SerializeField] private Image cableIconImg;              // 케이블/번개 아이콘

    [Header("게임 클리어 연출")]
    [SerializeField] private Image captureImg;
    [SerializeField] private RawImage snapshotImg;    // 캡쳐된 화면을 보여줄 RawImage
    [SerializeField] private RectTransform galleryBtnTarget; // 사진이 날아갈 목표(도감 버튼)

    [Header("워드이터 관련")]
    [SerializeField] private RectTransform WordEaterPanel;
    [SerializeField] private RectTransform WordEaterBtn;

    [Header("히스토리 관련")]
    [SerializeField] private RectTransform HistoryPanel;
    [SerializeField] private RectTransform HistoryBtn;

    [Header("잠금 힌트 관련")]
    [SerializeField] private RectTransform LockPanel;
    [SerializeField] private RectTransform LockBtn; // 홈화면 아이콘 버튼의 RectTransform
    [SerializeField] private LockHintAppController lockController;


    [Header("UI 연결")]
    [SerializeField] private ADPopup sharedAdPopup;

    public string HistoryLIne = "";
    public string RelevantLine = "";
    public List<string> RelevantResult = new List<string>();
    public bool isLength = false;
    public bool isFirst = false;
    public bool isLast = false;
    public bool isChoseongItem = false;

    [Header("슬라이드 메니저")]
    [SerializeField] private SlideManager smanager;


    [Header("횟수")]
    public int callCount = 0;
    public int msgCount = 0;
    public int submitCount = 0;
    public int lockCount = 0;

    public int MaxRelevant = 8;

    [Header("Info 디버그용 변수")]
    public Vector3 InfoDebug;
    public GameObject temp;


    public static GameManager Instance;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        // 파일들 먼저 불러오기 (여기서 wordEater의 Stage가 결정됨)
        filemanager.LoadWordEaterInfo();
        filemanager.LoadSoundInfo();
        filemanager.LoadCountData();

        string currentName = FileManager.Instance.CurrentPlayerName;
        bool isDefaultName = (currentName == "워드이터"); // 기본 이름인지 확인

        if (wordeater.CurrentStage == GrowthStage.Bit && isDefaultName)
        {
            wordEaterNamePanel.SetActive(true);
        }
        else
        {
            wordEaterNamePanel.SetActive(false);
        }

        // 시작 브금 출력
        SoundManager.Instance.BGMStart(1);

        // 게임오버 패널 초기화
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0;
            gameOverCanvasGroup.gameObject.SetActive(false);
        }
        InitBlurPanel(folderCanvasGroup, FolderPanel);
        InitBlurPanel(itemFolderCanvasGroup, ItemFolderPanel);

        // [추가] 워드이터 펫 동작 시작 (아이콘이 화면을 돌아다님)
        if (WordEaterBtn != null)
        {
            var pet = WordEaterBtn.GetComponent<WordEaterPet>();
            if (pet == null) pet = WordEaterBtn.gameObject.AddComponent<WordEaterPet>();
        }
    }

    // 블러 패널 초기화 헬퍼 함수
    private void InitBlurPanel(CanvasGroup cg, RectTransform rt)
    {
        if (cg != null && rt != null)
        {
            rt.gameObject.SetActive(true); // 무조건 켜둠 (셰이더 로드 유지)
            cg.alpha = 0f;                 // 대신 투명하게
            cg.blocksRaycasts = false;     // 터치 방지
            cg.interactable = false;
        }
    }
    /// <summary>
    /// [아이템 광고] 버튼 클릭 시
    /// </summary>
    public void OnClickGetItemAd()
    {
        if (sharedAdPopup == null) return;

        SoundManager.Instance.SFXStart(SoundManager.SFXType.button1);
        // 팝업 문구 설정 (아이템용 멘트로 변경)
        sharedAdPopup.Configure(
            title: "반짝이는 무언가 발견!",
            watchAdText: "광고보고 줍기",
            noThanksText: "그냥 가기"
        );

        // 팝업 띄우기 & 보상 로직 연결
        sharedAdPopup.YesNoPanelShow(
            onAccept: () =>
            {
                // 광고 시청 완료(Yes) 시 실행될 로직
                // Debug.Log("광고 보상: 랜덤 아이템 지급");
                ItemDropManager.Instance.ObtainRandomItem(); // 아이템 획득 함수 호출
            },
            onDecline: () =>
            {
                // 거절(No) 시 실행될 로직
                // Debug.Log("아이템 획득 거절");
            }
        );
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

            SoundManager.Instance.SFXStart(SoundManager.SFXType.dead);

            // 기존 텍스트 알림 대신 방전 연출 코루틴 실행
            StartCoroutine(ProcessGameOverSequence());
        }
        // 게임 클리어
        else if (type == 2)
        {
            touchblockPanel.SetActive(true);
            StartCoroutine(ProcessGameClearSequence());
        }
    }

    // 0 -> 길이만 , 1 -> 첫번째 초성 , 2 -> 두 번째 초성 , 3 -> 초성 아이템
    public void saveLock(int type)
    {
        switch (type)
        {
            case 0:
                isLength = true;
                break;
            case 1:
                isFirst = true;
                break;
            case 2:
                isLast = true;
                break;
            case 3:
                isChoseongItem = true;
                break;

            default:
                break;
        }

        filemanager.SaveLockHistoryInfo();
    }


    // type : 0 -> 전화 , 1 -> 메세지 , 2 - > 제출 , 3 -> 잠금
    public void saveCountInmanager(int type)
    {
        switch (type)
        {
            case 0:
                callCount++;
                break;

            case 1:
                msgCount++;
                break;

            case 2:
                submitCount++;
                break;

            case 3:
                lockCount++;
                break;

            default:
                break;
        }

        filemanager.SaveCountData(callCount, msgCount, submitCount, lockCount);
    }

    /// <summary>
    /// 게임 클리어 시 연출 처리
    /// </summary>
    private IEnumerator ProcessGameClearSequence()
    {
        // 1. 게임 클리어 텍스트 표시
        NoticeManager.Instance.ShowTimed("게임 클리어!", 2.0f);

        // 2. 잠시 대기
        yield return new WaitForSeconds(1.5f);

        // 3. [중요] 화면 캡쳐를 위해 프레임 끝까지 대기 (필수)
        yield return new WaitForEndOfFrame();

        SoundManager.Instance.SFXStart(SoundManager.SFXType.DogamAssign);

        // -------------------------------------------------------
        // A. 화면 캡쳐 및 이미지 할당
        // -------------------------------------------------------
        Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();

        if (snapshotImg != null)
        {
            snapshotImg.texture = screenTexture;
            snapshotImg.color = Color.white;
            snapshotImg.gameObject.SetActive(true);

            // 초기화: 화면 꽉 찬 상태, 중앙 위치
            snapshotImg.rectTransform.localScale = Vector3.one;
            snapshotImg.rectTransform.anchoredPosition = Vector2.zero;

            // 캔버스 크기에 맞춰 사이즈 델타 조정 (Stretch 상태면 생략 가능하지만 안전하게)
            snapshotImg.rectTransform.sizeDelta = Vector2.zero;
        }

        // -------------------------------------------------------
        // B. 하얀색 플래시 터트리기 (찰칵!)
        // -------------------------------------------------------
        if (captureImg != null)
        {
            captureImg.gameObject.SetActive(true);
            captureImg.color = Color.white; // 불투명

            // 0.5초 동안 빠르게 사라짐 -> 뒤에 있는 snapshotImg가 드러남
            captureImg.DOFade(0f, 0.5f).SetEase(Ease.OutQuad);
        }

        // 플래시가 걷히는 시간 대기
        yield return new WaitForSeconds(0.6f);

        // -------------------------------------------------------
        // C. 사진이 도감 버튼으로 빨려들어가는 연출
        // -------------------------------------------------------
        if (snapshotImg != null && galleryBtnTarget != null)
        {
            // 목표 위치 계산 (CanvasUtil 활용)
            // snapshotImg의 부모 기준으로 galleryBtnTarget의 위치를 가져옴
            Vector2 targetLocalPos = CanvasUtil.ConvertBetweenCanvases(
                galleryBtnTarget,
                snapshotImg.rectTransform.parent as RectTransform
            );

            // DOTween 시퀀스 생성
            Sequence flySeq = DOTween.Sequence();

            // 1. 위치 이동
            flySeq.Join(snapshotImg.rectTransform.DOAnchorPos(targetLocalPos, 1.0f).SetEase(Ease.InBack));

            // 2. 크기 축소 (작은 사진처럼)
            flySeq.Join(snapshotImg.rectTransform.DOScale(0.1f, 1.0f).SetEase(Ease.InBack));

            // 3. 마지막에 살짝 페이드 아웃
            flySeq.Join(snapshotImg.DOFade(0f, 0.3f).SetDelay(0.7f));

            // 애니메이션 끝날 때까지 대기
            yield return flySeq.WaitForCompletion();

            // 이미지 끄기
            snapshotImg.gameObject.SetActive(false);
        }

        // [메모리 관리] 캡쳐한 텍스처 메모리 해제 (중요!)
        if (screenTexture != null)
        {
            Destroy(screenTexture);
        }
        // 사진이 갤러리에 들어간 뒤, 유저가 "아 저장됐구나" 인식할 시간(0.5~1초)을 줍니다.
        yield return new WaitForSeconds(0.8f);

        // -------------------------------------------------------
        // D. 아이템 획득 및 재시작
        // -------------------------------------------------------

        // 아이템 획득 로직
        ItemDropManager.Instance.ObtainRandomItem(true);

        // 아이템 확인 시간
        yield return new WaitForSeconds(2.5f);

        // 플래시 이미지 안전하게 끄기
        if (captureImg != null) captureImg.gameObject.SetActive(false);

        // 게임 재시작
        Restart();
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

    public void Restart()
    {
        touchblockPanel.SetActive(false);

        // 워드이터 단계 초기화
        wordeater.BeginStage(GrowthStage.Bit, initial: true);

        // [중요] 죽어서 다시 시작하므로, 이름을 기본값으로 되돌려야 입력창이 뜸
        // 파일매니저를 통해 이름만 "워드이터"로 리셋 (저장은 나중에 입력할 때 됨)
        FileManager.Instance.SetPlayerName("워드이터");

        // [중요] 게임 재시작 시 1번(0번 인덱스) 화면으로 이동
        if (phoneSwiper != null)
        {
            phoneSwiper.GoToPage(0);
        }

        // 패널 켜기
        if (wordEaterNamePanel != null)
        {
            wordEaterNamePanel.SetActive(true);
        }
    }


    public void UpdateHistoryLineInFile(string newHis)
    {
        filemanager.SaveHistory(newHis);
    }



    // ---- 공용 유틸 애니메이션 ----
    private void ShowPanelFromButton(RectTransform panel, RectTransform btn)
    {
        if (panel == null || btn == null) return;
        SoundManager.Instance.SFXStart(SoundManager.SFXType.button1);
        smanager.isOK = false;
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
        phoneSwiper.isUsingTab = true;
    }

    private void HidePanelToButton(RectTransform panel, RectTransform btn)
    {
        if (panel == null || btn == null) return;

        SoundManager.Instance.SFXStart(SoundManager.SFXType.button2);

        var parent = panel.parent as RectTransform;
        Vector2 endLocal = CanvasUtil.ConvertBetweenCanvases(btn, parent);
        panel.DOScale(Vector3.zero, 0.2f)
                     .SetEase(Ease.InBack)
                     .SetUpdate(true);

        panel.DOAnchorPos(endLocal, 0.2f)
             .SetEase(Ease.InBack)
             .SetUpdate(true)
             .OnComplete(() =>
             {
                 panel.gameObject.SetActive(false);
                 smanager.isOK = true;
                 smanager.BlockJJS = false;
             });
        phoneSwiper.isUsingTab = false;
    }

    // ---- 단일 인자 버전 (같은 Canvas에서만 사용 시) ----
    public void ShowPanel(RectTransform panel)
    {
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.zero;
        panel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        SoundManager.Instance.SFXStart(SoundManager.SFXType.button1);
    }

    public void HidePanel(RectTransform panel)
    {
        if (panel == null) return;
        panel.DOScale(Vector3.zero, 0.2f)
             .SetEase(Ease.InBack)
             .OnComplete(() => panel.gameObject.SetActive(false));

        SoundManager.Instance.SFXStart(SoundManager.SFXType.button2);
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
        // 연출 중단
        if (ringingCoroutine != null)
        {
            StopCoroutine(ringingCoroutine);
            ringingCoroutine = null;
        }

        // 흔들림 때문에 돌아간 회전값/위치값 초기화
        CallPanel.transform.rotation = Quaternion.identity;

        // 패널 퇴장 (기존 함수)
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
            CallPanel2.DOShakeRotation(0.5f, 30f, 10, 10, true);

            // 다음 진동까지 대기 (진동 간격)
            // 1초 쉬고 다시 울림 (따르릉~ ... 따르릉~ 느낌)
            yield return new WaitForSeconds(1.2f);
        }
    }

    //type이 0 -> 위에서 아래로 슬라이드, 1-> 아래에서 위로 슬라이드
    public void SlidePanelSetting(RectTransform Panel, Vector2 originPos, int type)
    {
        float duration = 0.4f;

        //위에서 아래로
        if (type == 0)
        {
            smanager.isOK = false;
            Panel.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            Panel.gameObject.SetActive(true);

            Panel.DOAnchorPos(originPos, duration)
                .SetEase(Ease.OutCubic);
        }
        //아래에서 위로
        else if (type == 1)
        {
            Panel.DOAnchorPos(originPos + Vector2.up * Screen.height, duration)
                         .SetEase(Ease.InCubic)
                     .OnComplete(() =>
                      {
                          Panel.gameObject.SetActive(false);
                          smanager.isOK = true;
                          smanager.BlockJJS = false;
                          phoneSwiper.isUsingTab = false;
                      });
        }
    }

    public void SlidePanelDuring(RectTransform Panel, Vector2 targetPos)
    {

        float duration = 0.2f;

        Panel.DOAnchorPos(targetPos, duration)
                    .SetEase(Ease.OutCubic);
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

    public void ShowPanel_Setting() => ShowPanelFromButton(SettingPanel, SettingBtn);
    public void HidePanel_Setting() => HidePanelToButton(SettingPanel, SettingBtn);

    public void ShowPanel_WordEater() => ShowPanelFromButton(WordEaterPanel, WordEaterBtn);
    public void HidePanel_WordEater() => HidePanelToButton(WordEaterPanel, WordEaterBtn);

    public void ShowPanel_History() => ShowPanelFromButton(HistoryPanel, HistoryBtn);
    public void HidePanel_History() => HidePanelToButton(HistoryPanel, HistoryBtn);

    public void ShowPanel_Folder() => ShowBlurPanelFromButton(FolderPanel, FolderBtn, folderCanvasGroup);
    public void HidePanel_Folder() => HideBlurPanelToButton(FolderPanel, FolderBtn, folderCanvasGroup);

    public void ShowPanel_Item() => ShowBlurPanelFromButton(ItemFolderPanel, ItemFolderBtn, itemFolderCanvasGroup);
    public void HidePanel_Item(bool playSound = true) 
    {
        // 소리를 재생하라고 했을 때만 재생
        if (playSound)
        {
            SoundManager.Instance.SFXStart(SoundManager.SFXType.button2);
        }
        
        HideBlurPanelToButton(ItemFolderPanel, ItemFolderBtn, itemFolderCanvasGroup);
    }
    /// <summary>
    /// 잠금 패널 열기
    /// </summary>
    public void ShowPanel_Lock()
    {
        // 힌트 컨트롤러 실행 및 결과 받기
        // OpenLockHint() 내부에서 중복 체크 -> 배터리 소모 -> 힌트 생성
        bool isNewHintGenerated = lockController.OpenLockHint();

        // 새로운 힌트가 생성되었을 때만 패널을 열어줌
        if (isNewHintGenerated)
        {
            ShowPanelFromButton(LockPanel, LockBtn);
        }

    }

    public void HidePanel_Lock()
    {
        HidePanelToButton(LockPanel, LockBtn);
    }

    // -----------------------------------------------------------------------
    // [핵심 변경 2] 블러 패널 전용 Show/Hide 함수 추가
    // 기존 ShowPanelFromButton과 비슷하지만 SetActive 대신 Alpha를 조절함
    // -----------------------------------------------------------------------

    private void ShowBlurPanelFromButton(RectTransform panel, RectTransform btn, CanvasGroup cg)
    {
        if (panel == null || btn == null || cg == null) return;

        SoundManager.Instance.SFXStart(SoundManager.SFXType.button1);
        smanager.isOK = false;

        // [중요] 이미 SetActive(true) 상태임. 초기화 렉 없음.

        var parent = panel.parent as RectTransform;
        Vector2 startLocal = CanvasUtil.ConvertBetweenCanvases(btn, parent);

        // 애니메이션 시작 전 위치/스케일 초기화
        panel.anchoredPosition = startLocal;
        panel.localScale = Vector3.zero;

        // 터치 활성화
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // DOTween 시퀀스로 부드럽게 등장 (투명도 + 크기 + 이동)
        Sequence seq = DOTween.Sequence();
        seq.Join(panel.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutBack)); // 중앙으로 이동
        seq.Join(panel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));      // 커지기
        seq.Join(cg.DOFade(1f, 0.3f));                                         // [핵심] 투명도 0->1

        phoneSwiper.isUsingTab = true;
    }

    /// <summary>
    /// [재적용] 애니메이션 없이 즉시 블러 패널들을 닫음 (결과창 가림 방지)
    /// </summary>
    public void CloseBlurPanelsImmediate()
    {
        // Debug.Log("[GameManager] CloseBlurPanelsImmediate Called!");

        if (FolderPanel != null)
        {
            FolderPanel.DOKill();
            FolderPanel.gameObject.SetActive(false);
        }

        if (ItemFolderPanel != null)
        {
            ItemFolderPanel.DOKill();
            ItemFolderPanel.gameObject.SetActive(false);
        }

        // 블러 캔버스 그룹도 리셋
        if (folderCanvasGroup != null)
        {
            folderCanvasGroup.DOKill();
            folderCanvasGroup.alpha = 0f;
            folderCanvasGroup.blocksRaycasts = false;
            folderCanvasGroup.interactable = false;
        }
        if (itemFolderCanvasGroup != null)
        {
            itemFolderCanvasGroup.DOKill();
            itemFolderCanvasGroup.alpha = 0f;
            itemFolderCanvasGroup.blocksRaycasts = false;
            itemFolderCanvasGroup.interactable = false;
        }

        if (phoneSwiper != null) phoneSwiper.isUsingTab = false;
    }

    private void HideBlurPanelToButton(RectTransform panel, RectTransform btn, CanvasGroup cg)
    {
        // [수정] 참조가 없더라도 일단 스와이프 잠금은 해제해야 함 (버그 방지)
        if (phoneSwiper != null) phoneSwiper.isUsingTab = false;

        SoundManager.Instance.SFXStart(SoundManager.SFXType.button2);
        if (panel == null || btn == null || cg == null) return;

        var parent = panel.parent as RectTransform;
        Vector2 endLocal = CanvasUtil.ConvertBetweenCanvases(btn, parent);

        // 터치 즉시 비활성화
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(panel.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        seq.Join(panel.DOAnchorPos(endLocal, 0.2f).SetEase(Ease.InBack));
        seq.Join(cg.DOFade(0f, 0.2f)); // [핵심] 투명도 1->0

        seq.SetUpdate(true);
        seq.OnComplete(() =>
        {
            smanager.isOK = true;
            smanager.BlockJJS = false;
        });

        phoneSwiper.isUsingTab = false;
    }



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



