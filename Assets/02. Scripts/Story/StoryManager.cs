using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video; // [추가] 비디오 플레이어용
using TMPro; 
using DG.Tweening; 

public enum StoryEffectType
{
    None,
    ImageMove, // 투명도 0->1 되면서 목표 위치로 이동
    VideoPlay  // [추가] 비디오 재생 (다음 텍스트 시 종료)
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
    public VideoPlayer videoPlayer; // [추가] 재생할 비디오 플레이어
}

public class StoryManager : MonoBehaviour
{
    // ... (변수들 생략 - 기존 유지)
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI storyText;  // 대사 출력용
    [SerializeField] private Button screenButton;        // 전체 화면 클릭용 버튼
    [SerializeField] private Image bgImage;              // 배경 이미지 (페이드 아웃용)

    [Header("MiniGame References")]
    [SerializeField] private GameObject miniGameRoot;    // 미니게임 오브젝트 그룹
    [SerializeField] private Image fillImage;            // 차오를 게이지 이미지 (Image Type: Filled)
    [SerializeField] private Image targetChangeImage;    // 색이 바뀔 대상 이미지
    [SerializeField] private GameObject hiddenObj;       // 100% 달성 시 켜질 오브젝트
    [SerializeField] private Color targetColor = Color.red; // 바뀔 목표 색상

    [Header("Story Data")]
    [SerializeField] private List<StoryStep> storyData;  // 대사 리스트
    [SerializeField] private float typingSpeed = 0.05f;  // 글자 나오는 속도

    // ... (내부 변수 생략 - 기존 유지)
    private int currentIndex = 0;
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;

    // 미니게임 상태
    private bool isMiniGameActive = false;
    private float currentFill = 0f;

    void Start()
    {
        // ... (기존 Start 내용 유지, 너무 기니까 여기선 생략하고 덮어쓰지 않음, 아 아래에 전체 다시 쓰는게 낫겠다)
        // 리스너 연결
        if (screenButton != null)
        {
            screenButton.onClick.RemoveAllListeners();
            screenButton.onClick.AddListener(OnScreenClick);
            Debug.Log("[StoryManager] Screen Button 연결 완료");
        }
        else
        {
            Debug.LogError("[StoryManager] Screen Button이 연결되지 않았습니다! 인스펙터를 확인하세요.");
        }

        // 초기화
        if (miniGameRoot != null) miniGameRoot.SetActive(false);
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (hiddenObj != null) hiddenObj.SetActive(false);

        // 첫 대사 시작
        if (storyData != null && storyData.Count > 0)
        {
            PlayStep(0);
        }
        else
        {
            Debug.LogWarning("[StoryManager] Story Data가 비어있습니다.");
        }
    }

    // ... (OnScreenClick 유지)

    // 화면 클릭 시 처리
    private void OnScreenClick()
    {
        Debug.Log($"[StoryManager] 화면 클릭됨! (Typing: {isTyping}, MiniGame: {isMiniGameActive})");

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
            Debug.Log("[StoryManager] 스토리 종료");
            // 씬 전환이나 엔딩 로직 등을 여기에 추가
            return;
        }

        // [추가] 이전 단계가 비디오 재생이었다면 끄기 (사라지게 함)
        if (currentIndex < storyData.Count)
        {
            var prevStep = storyData[currentIndex];
            // 방금 끝난 스텝이 이번 스텝과 다르고(즉 넘어가는 중이고)
            // 비디오 타입이었다면 정리
            if (index != currentIndex && prevStep.effectType == StoryEffectType.VideoPlay)
            {
                if (prevStep.videoPlayer != null)
                {
                    prevStep.videoPlayer.Stop();
                    prevStep.videoPlayer.gameObject.SetActive(false);
                }
            }
        }

        currentIndex = index;
        StoryStep step = storyData[index];
        currentFullText = step.text;

        // [추가] 이펙트가 있다면 실행 (텍스트 출력과 동시 시작)
        CheckAndPlayEffect(step);

        // 텍스트 타이핑 시작
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(RoutineTypeWriter(currentFullText));
    }

    // [추가] 이펙트 재생 로직
    private void CheckAndPlayEffect(StoryStep step)
    {
        // 1. 이미지 이동
        if (step.effectType == StoryEffectType.ImageMove)
        {
            float duration = step.effectDuration > 0 ? step.effectDuration : 1.0f;

            for (int i = 0; i < step.effectImages.Count; i++)
            {
                if (step.effectTargets == null || i >= step.effectTargets.Count) break;

                RectTransform imgRect = step.effectImages[i];
                RectTransform targetRect = step.effectTargets[i];

                if (imgRect != null && targetRect != null)
                {
                    imgRect.gameObject.SetActive(true);

                    Image imgComponent = imgRect.GetComponent<Image>();
                    if (imgComponent != null)
                    {
                        Color c = imgComponent.color;
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

                    imgRect.DOMove(targetRect.position, duration).SetEase(Ease.OutQuad);
                }
            }
        }
        // 2. 비디오 재생
        else if (step.effectType == StoryEffectType.VideoPlay)
        {
            if (step.videoPlayer != null)
            {
                step.videoPlayer.gameObject.SetActive(true);
                step.videoPlayer.Play();
            }
        }
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
        Debug.Log("미니게임 시작!");
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
        Debug.Log("미니게임 클리어!");

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

        // [수정] 3. 여기서 다음 텍스트를 먼저 뱉는다
        PlayStep(currentIndex + 1);

        // [수정] 4. 텍스트 나온 후 0.5초 대기
        yield return new WaitForSeconds(0.5f);

        // [수정] 5. 배경 Fade Out 시작과 동시에 잔상 UI 삭제
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
