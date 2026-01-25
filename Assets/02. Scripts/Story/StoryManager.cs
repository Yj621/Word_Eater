using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video; 
using TMPro; 
using DG.Tweening; 

public enum StoryEffectType
{
    None,
    ImageMove, // 투명도 0->1 되면서 목표 위치로 이동
    VideoPlay,  // 비디오 재생 (명시적 종료 전까지 유지)
    ImageOff   // [추가] 지정된 이미지 끄기
}

[System.Serializable]
public class StoryStep
{
    [TextArea] public string text;      // 출력할 대사
    public bool triggerMiniGame;        // 이 대사가 끝나면 미니게임 시작할지 여부
    
    [Header("Effect Settings")]
    public StoryEffectType effectType;
    public List<RectTransform> effectImages; // 이동할 이미지들
    public List<RectTransform> effectTargets; // 목적지 (위치 참조용)
    public float effectDuration = 1.0f;

    [Header("Video Settings")]
    public VideoPlayer videoPlayer; // 재생할 비디오 플레이어
}

public class StoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI storyText;  // 대사 출력용
    [SerializeField] private Button screenButton;        // 전체 화면 클릭용 버튼
    [SerializeField] private Image bgImage;              // 배경 이미지 (페이드 아웃용)
    [SerializeField] private RawImage globalVideoDisplay; // [New] 전체 화면 비디오 출력용 RawImage

    [Header("MiniGame References")]
    [SerializeField] private GameObject miniGameRoot;    // 미니게임 오브젝트 그룹
    [SerializeField] private Image fillImage;            // 차오를 게이지 이미지 (Image Type: Filled)
    [SerializeField] private Image targetChangeImage;    // 색이 바뀔 대상 이미지
    [SerializeField] private GameObject hiddenObj;       // 100% 달성 시 켜질 오브젝트
    [SerializeField] private Color targetColor = Color.red; // 바뀔 목표 색상

    [Header("Story Data")]
    [SerializeField] private List<StoryStep> storyData;  // 대사 리스트
    [SerializeField] private float typingSpeed = 0.05f;  // 글자 나오는 속도

    // 내부 변수
    private int currentIndex = 0;
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;

    // 미니게임 상태
    private bool isMiniGameActive = false;
    private float currentFill = 0f;

    // 현재 화면에 띄워진 이펙트 이미지들 추적용 (필요시 전체 끄기 등을 위해 남겨둠)
    private List<RectTransform> _activeImages = new List<RectTransform>();
    
    // [추가] 현재 재생 중인 비디오 추적용
    private VideoPlayer _currentVideoPlayer;

    [Header("Scene Transition")]
    [SerializeField] private Image fadeOutImage; // 끝나고 나갈 때 어두워질 이미지 (검은색 Panel 권장)

    void Start()
    {
        // 1. 리스너 연결
        if (screenButton != null)
        {
            screenButton.onClick.RemoveAllListeners();
            screenButton.onClick.AddListener(OnScreenClick);
            // Debug.Log("[StoryManager] Screen Button 연결 완료");
        }
        else
        {
            // Debug.LogError("[StoryManager] Screen Button이 연결되지 않았습니다! 인스펙터를 확인하세요.");
        }

        // 2. 미니게임 UI 초기화
        if (miniGameRoot != null) miniGameRoot.SetActive(false);
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (hiddenObj != null) hiddenObj.SetActive(false);

        // 3. [중요] 모든 StoryStep의 이미지/비디오를 사전에 초기화 (전부 끄기)
        InitializeAllStoryResources();
        
        // 페이드 아웃 이미지가 켜져있다면 끄기 (씬 진입 효과용으로 쓸 수도 있으나 여기선 생략)
        if (fadeOutImage != null) fadeOutImage.gameObject.SetActive(false);

        // 4. 첫 대사 시작
        if (storyData != null && storyData.Count > 0)
        {
            PlayStep(0);
        }
        else
        {
            // Debug.LogWarning("[StoryManager] Story Data가 비어있습니다.");
        }
    }

    /// <summary>
    /// 게임 시작 시 씬에 배치된 모든 이펙트 이미지와 비디오를 숨기고 초기화함.
    /// (처음부터 떠 있거나 켜져있는 문제 방지)
    /// </summary>
    private void InitializeAllStoryResources()
    {
        // 글로벌 비디오 화면 초기화 (투명)
        if (globalVideoDisplay != null)
        {
            globalVideoDisplay.gameObject.SetActive(true);
            SetRawImageAlpha(globalVideoDisplay, 0f);
        }

        if (storyData == null) return;

        foreach (var step in storyData)
        {
            // 비디오 끄기
            if (step.videoPlayer != null)
            {
                step.videoPlayer.Stop();
                step.videoPlayer.targetTexture?.Release(); // 혹시 RenderTexture 쓴다면
                step.videoPlayer.gameObject.SetActive(false);
            }

            // 이미지들 끄기 및 투명도 0
            if (step.effectImages != null)
            {
                foreach (var img in step.effectImages)
                {
                    if (img == null) continue;
                    
                    // DOTween 애니메이션 중지
                    img.DOKill();
                    
                    // 투명도 0
                    var imgComp = img.GetComponent<Image>();
                    if (imgComp) 
                    {
                        var c = imgComp.color;
                        c.a = 0f;
                        imgComp.color = c;
                    }
                    var cg = img.GetComponent<CanvasGroup>();
                    if (cg) cg.alpha = 0f;

                    // 비활성화
                    img.gameObject.SetActive(false);
                }
            }
        }
    }

    // 화면 클릭 시 처리
    private void OnScreenClick()
    {
        // Debug.Log($"[StoryManager] 화면 클릭됨! (Typing: {isTyping}, MiniGame: {isMiniGameActive})");

        // 1. 미니게임 중일 때
        if (isMiniGameActive)
        {
            HandleMiniGameClick();
            return;
        }

        // 2. 텍스트 타이핑 중일 때 -> 스킵
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            storyText.text = currentFullText;
            isTyping = false;
            
            // 타이핑 스킵 직후 미니게임 체크 해야 할 수도 있음
            CheckPostStepTrigger();
            return;
        }

        // 3. 대사 완성 후 대기 중 -> 다음 단계로
        NextStep();
    }

    private void PlayStep(int index)
    {
        if (index >= storyData.Count)
        {
            // Debug.Log("[StoryManager] 스토리 종료");
            EndStory();
            return;
        }

        // [수정] "자동 정리" 로직 삭제. 
        // 사용자가 "계속 켜두다가"를 원하므로, 스텝 넘어갈 때 끄지 않음.

        currentIndex = index;
        StoryStep step = storyData[index];
        currentFullText = step.text;

        // [추가] 이펙트가 있다면 실행 (텍스트 출력과 동시 시작)
        CheckAndPlayEffect(step);

        // 텍스트 타이핑 시작
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(RoutineTypeWriter(currentFullText));
    }
    
    // 스토리 종료 -> 로딩 씬 -> 메인
    private void EndStory()
    {
        // 1. 시청 완료 기록
        PlayerPrefs.SetInt("HasWatchStory", 1);
        PlayerPrefs.Save();

        // 2. 페이드 후 이동
        if (fadeOutImage != null)
        {
            fadeOutImage.gameObject.SetActive(true);
            // 투명 -> 검정(불투명)
            var c = fadeOutImage.color;
            c.a = 0f; 
            fadeOutImage.color = c;

            fadeOutImage.DOFade(1f, 1.0f).OnComplete(() =>
            {
                LoadingSceneManager.LoadScene("WordEater");
            });
        }
        else
        {
            LoadingSceneManager.LoadScene("WordEater");
        }
    }

    // [추가] 이펙트 재생 로직
    private void CheckAndPlayEffect(StoryStep step)
    {
        // 1. 이미지 이동 (켜기)
        if (step.effectType == StoryEffectType.ImageMove)
        {
            // [요구사항] "동영상이 켜진 시점에서 어느 텍스트든 이미지가 나오면 동영상을 꺼야해"
            if (_currentVideoPlayer != null)
            {
                _currentVideoPlayer.Stop();
                _currentVideoPlayer.gameObject.SetActive(false);
                _currentVideoPlayer = null;
                
                // 글로벌 스크린 투명화
                SetRawImageAlpha(globalVideoDisplay, 0f);
            }

            float duration = step.effectDuration > 0 ? step.effectDuration : 1.0f;

            for (int i = 0; i < step.effectImages.Count; i++)
            {
                // 짝 맞추기 (Target이 없으면 제자리 혹은 활성화만 할 수도 있으나 기존 로직 유지)
                if (step.effectTargets == null || i >= step.effectTargets.Count) break;

                RectTransform imgRect = step.effectImages[i];
                RectTransform targetRect = step.effectTargets[i];

                if (imgRect != null && targetRect != null)
                {
                    // 추적 리스트에 추가
                    if (!_activeImages.Contains(imgRect)) _activeImages.Add(imgRect);

                    // 활성화 및 애니메이션 시작
                    imgRect.gameObject.SetActive(true);
                    imgRect.DOKill(); 

                    // 투명도 초기화 (0에서 시작)
                    Image imgComponent = imgRect.GetComponent<Image>();
                    if (imgComponent != null)
                    {
                        var c = imgComponent.color;
                        c.a = 0f;
                        imgComponent.color = c;
                        imgComponent.DOFade(1f, duration);
                    }
                    else 
                    {
                        CanvasGroup cg = imgRect.GetComponent<CanvasGroup>();
                        if(cg != null) 
                        {
                            cg.alpha = 0f;
                            cg.DOFade(1f, duration);
                        }
                    }

                    // 위치 이동
                    imgRect.DOMove(targetRect.position, duration).SetEase(Ease.OutQuad);
                }
            }
        }
        // 2. 비디오 재생
        else if (step.effectType == StoryEffectType.VideoPlay)
        {
            if (step.videoPlayer != null)
            {
                // 기존 재생 중인게 있다면 교체 (혹은 끄기)
                if (_currentVideoPlayer != null && _currentVideoPlayer != step.videoPlayer)
                {
                    _currentVideoPlayer.Stop();
                    _currentVideoPlayer.gameObject.SetActive(false);
                    SetRawImageAlpha(globalVideoDisplay, 0f);
                }

                _currentVideoPlayer = step.videoPlayer;
                _currentVideoPlayer.gameObject.SetActive(true);
                
                // [New] 글로벌 RawImage 켜기 및 텍스쳐 연결
                if (globalVideoDisplay != null)
                {
                    // 비디오 플레이어가 RenderTexture에 쏘는 경우 연결 필요
                    if (step.videoPlayer.targetTexture != null)
                        globalVideoDisplay.texture = step.videoPlayer.targetTexture;

                    globalVideoDisplay.gameObject.SetActive(true);
                    SetRawImageAlpha(globalVideoDisplay, 1f);
                }
                
                _currentVideoPlayer.Play();
            }
        }
        // 3. [추가] 이미지 끄기
        else if (step.effectType == StoryEffectType.ImageOff)
        {
            // effectImages 리스트에 있는 녀석들을 끈다.
            if (step.effectImages != null)
            {
                foreach (var img in step.effectImages)
                {
                    if (img != null)
                    {
                        img.DOKill();
                        img.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetRawImageAlpha(RawImage img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private IEnumerator RoutineTypeWriter(string fullText)
    {
        isTyping = true;
        storyText.text = "";

        foreach (char c in fullText)
        {
            storyText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        CheckPostStepTrigger();
    }

    // 텍스트 출력이 끝난(혹은 스킵한) 직후 체크
    private void CheckPostStepTrigger()
    {
        StoryStep step = storyData[currentIndex];
        
        // 이 단계가 미니게임 트리거라면?
        if (step.triggerMiniGame)
        {
            StartMiniGame();
        }
    }

    private void NextStep()
    {
        // 미니게임 중이면 클릭으로 다음 대사 넘어가지 않음 (미니게임 로직이 우선)
        if (isMiniGameActive) return;

        PlayStep(currentIndex + 1);
    }

    // =========================================================
    // 미니게임 로직
    // =========================================================

    private void StartMiniGame()
    {
        // Debug.Log("미니게임 시작!");
        isMiniGameActive = true;
        currentFill = 0f;

        if (miniGameRoot != null) miniGameRoot.SetActive(true);
        if (fillImage != null) fillImage.fillAmount = 0f;
    }

    private void HandleMiniGameClick()
    {
        // 채우기
        currentFill += 0.1f;
        if (currentFill > 1.0f) currentFill = 1.0f;

        if (fillImage != null)
            fillImage.fillAmount = currentFill;

        // 100% 달성 체크
        if (currentFill >= 1.0f)
        {
            StartCoroutine(RoutineMiniGameClear());
        }
    }

    private IEnumerator RoutineMiniGameClear()
    {
        isMiniGameActive = false; // 더 이상 클릭 안 먹힘
        // Debug.Log("미니게임 클리어!");

        // 1. 이미지 색 변경
        if (targetChangeImage != null)
        {
            targetChangeImage.color = targetColor;
        }

        // 2. 꺼져있던 이미지 켜기
        if (hiddenObj != null)
        {
            hiddenObj.SetActive(true);
        }

        // 1초 대기 (색 바뀐거 감상 시간)
        yield return new WaitForSeconds(1.0f);

        // 3. 여기서 다음 텍스트를 먼저 뱉는다
        PlayStep(currentIndex + 1);

        // 4. 텍스트 나온 후 0.5초 대기
        yield return new WaitForSeconds(0.5f);

        // 5. 배경 Fade Out 시작과 동시에 잔상 UI 삭제
        if (miniGameRoot != null) miniGameRoot.SetActive(false);

        if (bgImage != null)
        {
            // 부드럽게 사라짐
            yield return bgImage.DOFade(0f, 1.0f).WaitForCompletion();
            // 완전히 끔
            bgImage.gameObject.SetActive(false);
        }
    }
}
