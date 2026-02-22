using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class WordEaterPet : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("펫 설정")]
    [SerializeField] private float moveSpeed = 100f; 
    [SerializeField] private float waitMin = 2f;
    [SerializeField] private float waitMax = 5f;
    [SerializeField] private float padding = 50f;
    
    [Header("충돌/회피 설정")]
    [SerializeField] private float hitboxScale = 0.6f; 
    [SerializeField] private float pushDistance = 150f; 
    [SerializeField] private float pushDuration = 0.4f;

    [Header("드래그 설정")]
    [SerializeField] private float dragThreshold = 10f; // 이만큼 이상 움직여야 드래그로 인정

    [Header("추가 장애물 (직접 할당)")]
    public List<RectTransform> extraObstacles = new List<RectTransform>();

    [Header("애니메이션 설정")]
    [SerializeField] private Sprite[] defaultAnimSprites; // 기본 애니메이션 스프라이트들
    [SerializeField] private float animFrameRate = 0.2f;  // 프레임 교체 간격

    [Header("이동 범위 (비워두면 부모 영역 전체)")]
    public RectTransform roamingArea;

    private RectTransform rectTr;
    private RectTransform parentRect;
    private Coroutine behaviorRoutine;
    private Coroutine animRoutine; // 애니메이션 코루틴
    private PhoneSwiper phoneSwiper;
    private SlideManager slideManager;
    private Canvas canvas;
    private LayoutElement layoutElement; 
    private Image targetImage; // 애니메이션 적용할 이미지
    private Button attachedButton; // [추가] 클릭 제어를 위한 버튼 참조

    // 드래그 관련
    private bool isDragging = false;
    private bool isDragMoved = false; // 실제로 의미있게 움직였는지
    private Vector2 dragStartPos;
    private Vector2 dragOffset;
    
    // 충돌 감지 대상
    private List<RectTransform> obstacles = new List<RectTransform>();

    private void Awake()
    {
        rectTr = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>(); 
        attachedButton = GetComponent<Button>(); // [추가] 버튼 컴포넌트 가져오기

#if UNITY_2023_1_OR_NEWER
        phoneSwiper = FindFirstObjectByType<PhoneSwiper>();
        slideManager = FindFirstObjectByType<SlideManager>();
#else
        phoneSwiper = FindObjectOfType<PhoneSwiper>();
        slideManager = FindObjectOfType<SlideManager>();
#endif
        canvas = GetComponentInParent<Canvas>();
        
        // 레이아웃 엘리먼트 확보 (없으면 추가)
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    private void Start()
    {
        // 1. 기존 DraggableIcon 제거
        var oldDrag = GetComponent<DraggableIcon>();
        if (oldDrag != null) Destroy(oldDrag);

        // 2. 부모 레이아웃의 영향을 받지 않도록 설정
        layoutElement.ignoreLayout = true;

        if (transform.parent != null)
        {
            parentRect = transform.parent as RectTransform;
            UpdateObstacles();
        }

        StartBehavior();
        StartAnimation(); // 애니메이션 시작
    }

    public void SetAnimSprites(Sprite[] sprites, float frameRate = -1f)
    {
        defaultAnimSprites = sprites;
        if (frameRate > 0) animFrameRate = frameRate;
        StartAnimation();
    }

    private void OnEnable()
    {
        StartBehavior();
        StartAnimation();
    }

    private void OnDisable()
    {
        StopBehavior();
        StopAnimation();
    }

    private void StartAnimation()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        if (defaultAnimSprites != null && defaultAnimSprites.Length > 0 && targetImage != null)
        {
            animRoutine = StartCoroutine(Routine_Animation());
        }
    }

    private void StopAnimation()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = null;
    }

    private IEnumerator Routine_Animation()
    {
        int index = 0;
        while (true)
        {
            if (targetImage != null && defaultAnimSprites.Length > 0)
            {
                targetImage.sprite = defaultAnimSprites[index];
                index = (index + 1) % defaultAnimSprites.Length;
            }
            yield return new WaitForSeconds(animFrameRate);
        }
    }
    
    private void UpdateObstacles()
    {
        obstacles.Clear();
        obstacles.AddRange(extraObstacles);
    }

    // ---- 드래그 구현 (IHandler) ----
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (phoneSwiper != null && phoneSwiper.isUsingTab) return;

        isDragging = true;
        isDragMoved = false; // 초기화
        dragStartPos = eventData.position; // 시작 위치 기록
        
        StopBehavior();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointer);
        
        dragOffset = rectTr.anchoredPosition - localPointer;

        // 드래그 시작 시점에는 아직 버튼을 끄지 않음 (살짝 움직인건 클릭으로 쳐주기 위해)
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || parentRect == null) return;

        // [추가] 일정 거리 이상 움직였으면 "드래그"로 간주하고 버튼 비활성화
        if (!isDragMoved)
        {
            if (Vector2.Distance(dragStartPos, eventData.position) > dragThreshold)
            {
                isDragMoved = true;
                if (attachedButton != null)
                {
                    // 버튼을 일시적으로 꺼서 클릭 이벤트 발생을 막음
                    attachedButton.enabled = false; 
                }
            }
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointer))
        {
            rectTr.anchoredPosition = localPointer + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        StartBehavior();

        // [추가] 드래그가 끝났으니 버튼 다시 복구
        if (attachedButton != null)
        {
            attachedButton.enabled = true;
        }
    }
    // ----------------------------

    // [변경] 부모를 교체하는 구조가 아닌, 배회 영역(roamingArea)만 새 페이지로 지정하여 펫이 그쪽으로 이동하게 함
    public void ChangeRoamingPage(RectTransform newPage)
    {
        if (newPage == null || roamingArea == newPage) return;
        
        roamingArea = newPage;
        
        // 이동 루틴을 재시작하여 즉각적으로 새로운 페이지 범위 내의 랜덤 위치로 이동하게 함
        StopBehavior();
        StartBehavior();
    }

    public void StartBehavior()
    {
        if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
        behaviorRoutine = StartCoroutine(Rutine_Roaming());
    }

    public void StopBehavior()
    {
        if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
        behaviorRoutine = null;
        transform.DOKill();
    }

    private IEnumerator Rutine_Roaming()
    {
        bool wasBusy = false;

        while (true)
        {
            // 0. 상태 체크
            bool isBusy = (phoneSwiper != null && phoneSwiper.isUsingTab) || 
                          (slideManager != null && !slideManager.isOK);

            if (isBusy)
            {
                wasBusy = true;
                transform.DOKill(); 
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if (wasBusy)
            {
                wasBusy = false;
                RectTransform hitObj = GetHittingObstacle();
                if (hitObj != null)
                {
                    yield return StartCoroutine(RepulsionMove(hitObj));
                }
            }

            // 1. 대기
            float waitTime = Random.Range(waitMin, waitMax);
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (slideManager != null && !slideManager.isOK) break; 
                
                elapsed += Time.deltaTime;
                
                RectTransform hitObj = GetHittingObstacle();
                if (hitObj != null)
                {
                    yield return StartCoroutine(RepulsionMove(hitObj));
                    elapsed = waitTime; 
                }
                
                yield return null;
            }

            if (slideManager != null && !slideManager.isOK) continue;


            if (parentRect == null)
            {
                parentRect = transform.parent as RectTransform;
                if (parentRect == null) yield break;
            }

            if (Random.value < 0.2f) UpdateObstacles();

            // 2. 이동 시작
            Vector3 targetPos = GetRandomLocalPosition();
            float distance = Vector2.Distance(rectTr.localPosition, targetPos);
            float duration = distance / moveSpeed;

            Tween moveTween = rectTr.DOLocalMove(targetPos, duration).SetEase(Ease.InOutQuad);

            while (moveTween != null && moveTween.IsActive() && moveTween.IsPlaying())
            {
                if ((phoneSwiper != null && phoneSwiper.isUsingTab) ||
                    (slideManager != null && !slideManager.isOK))
                {
                    moveTween.Kill();
                    break;
                }

                RectTransform hitObj = GetHittingObstacle();
                if (hitObj != null)
                {
                    moveTween.Kill();
                    yield return StartCoroutine(RepulsionMove(hitObj));
                    break;
                }

                yield return null;
            }
        }
    }

    private IEnumerator RepulsionMove(RectTransform obstacle)
    {
        Vector3 obsWorldPos = obstacle.position;
        Vector3 myWorldPos = rectTr.position;
        Vector3 myPos = rectTr.localPosition; 
        
        Vector3 worldDir = (myWorldPos - obsWorldPos).normalized;
        if (worldDir == Vector3.zero) worldDir = Random.insideUnitCircle.normalized;

        Vector3 localDir = worldDir;
        if (parentRect != null)
        {
            localDir = parentRect.InverseTransformDirection(worldDir); 
        }

        Vector3 target = myPos + localDir * pushDistance;

        RectTransform area = roamingArea != null ? roamingArea : parentRect;
        if (area != null)
        {
            Vector3[] corners = new Vector3[4];
            area.GetWorldCorners(corners);
            Vector3 bl = parentRect.InverseTransformPoint(corners[0]);
            Vector3 tr = parentRect.InverseTransformPoint(corners[2]);

            float minX = Mathf.Min(bl.x, tr.x) + padding;
            float maxX = Mathf.Max(bl.x, tr.x) - padding;
            float minY = Mathf.Min(bl.y, tr.y) + padding;
            float maxY = Mathf.Max(bl.y, tr.y) - padding;
            
            if (myPos.x < minX || myPos.x > maxX || myPos.y < minY || myPos.y > maxY)
            {
                 target = GetRandomLocalPosition(); 
            }
            else
            {
                if (target.x < minX) target.x = minX + pushDistance * 0.5f;
                if (target.x > maxX) target.x = maxX - pushDistance * 0.5f;
                if (target.y < minY) target.y = minY + pushDistance * 0.5f;
                if (target.y > maxY) target.y = maxY - pushDistance * 0.5f;
            }
        }

        yield return rectTr.DOLocalMove(target, pushDuration)
                           .SetEase(Ease.OutCubic)
                           .WaitForCompletion();
    }

    private RectTransform GetHittingObstacle()
    {
        Rect myRect = GetWorldRect(rectTr, hitboxScale);

        foreach (var obs in obstacles)
        {
            if (obs == null || !obs.gameObject.activeInHierarchy || obs == rectTr) continue;

            Rect obsRect = GetWorldRect(obs, hitboxScale);
            if (myRect.Overlaps(obsRect))
            {
                return obs;
            }
        }
        return null; 
    }

    private Rect GetWorldRect(RectTransform rt, float scale = 1.0f)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        
        float width = Mathf.Abs(corners[2].x - corners[0].x);
        float height = Mathf.Abs(corners[2].y - corners[0].y);
        
        float cx = (corners[0].x + corners[2].x) / 2f;
        float cy = (corners[0].y + corners[2].y) / 2f;

        float scaledW = width * scale;
        float scaledH = height * scale;

        return new Rect(cx - scaledW / 2f, cy - scaledH / 2f, scaledW, scaledH);
    }

    private Vector3 GetRandomLocalPosition()
    {
        RectTransform area = roamingArea != null ? roamingArea : parentRect;
        if (area == null) return transform.localPosition;

        Vector3[] corners = new Vector3[4];
        area.GetWorldCorners(corners);
        
        Vector3 bottomLeft = parentRect.InverseTransformPoint(corners[0]);
        Vector3 topRight = parentRect.InverseTransformPoint(corners[2]);

        float minX = Mathf.Min(bottomLeft.x, topRight.x) + padding;
        float maxX = Mathf.Max(bottomLeft.x, topRight.x) - padding;
        float minY = Mathf.Min(bottomLeft.y, topRight.y) + padding;
        float maxY = Mathf.Max(bottomLeft.y, topRight.y) - padding;

        if (minX > maxX) { float m = (minX + maxX) / 2; minX = m; maxX = m; }
        if (minY > maxY) { float m = (minY + maxY) / 2; minY = m; maxY = m; }

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);

        return new Vector3(x, y, 0f);
    }
}
