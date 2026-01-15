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

    // 드래그 관련
    private bool isDragging = false;
    private Vector2 dragOffset;
    
    // 충돌 감지 대상
    private List<RectTransform> obstacles = new List<RectTransform>();

    private void Awake()
    {
        rectTr = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>(); // 이미지 컴포넌트 가져오기

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

    // 외부에서 애니메이션 주입 가능하도록 열어둠
    public void SetAnimSprites(Sprite[] sprites, float frameRate = -1f)
    {
        defaultAnimSprites = sprites;
        if (frameRate > 0) animFrameRate = frameRate;
        StartAnimation();
    }

    // ... (중략) ...

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
        // [수정] 사용자가 직접 리스트에 넣은 것만 장애물로 인식 (자동 감지 제거)
        // 이렇게 하면 배경이나 엉뚱한 투명 패널에 반응하지 않음
        obstacles.AddRange(extraObstacles);
    }


    // ---- 드래그 구현 (IHandler) ----
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (phoneSwiper != null && phoneSwiper.isUsingTab) return;

        isDragging = true;
        StopBehavior();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointer);
        
        dragOffset = rectTr.anchoredPosition - localPointer;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || parentRect == null) return;

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
    }
    // ----------------------------

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
            // 0. 상태 체크 (탭 사용 중 or 아이콘 정리 중이면 대기)
            bool isBusy = (phoneSwiper != null && phoneSwiper.isUsingTab) || 
                          (slideManager != null && !slideManager.isOK);

            if (isBusy)
            {
                wasBusy = true;
                transform.DOKill(); 
                // 드래그 중엔 그냥 가만히 대기 (밀려나지 않음)
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // 바쁨 상태가 끝난 직후 (드롭 순간)
            if (wasBusy)
            {
                wasBusy = false;
                // 이제 내 위에 누가 있는지 확인하고 있으면 밀려남
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
                if (slideManager != null && !slideManager.isOK) break; // 누가 드래그 시작하면 즉시 대기 모드로
                
                elapsed += Time.deltaTime;
                
                // 대기 중에도 충돌 체크 (가만히 있는데 누가 와서 박으면? -> 사실 드래그 중이면 위 isBusy에서 걸림)
                // 하지만 혹시 모를 상황(다른 코드로 이동 등) 대비
                RectTransform hitObj = GetHittingObstacle();
                if (hitObj != null)
                {
                    yield return StartCoroutine(RepulsionMove(hitObj));
                    elapsed = waitTime; 
                }
                
                yield return null;
            }

            // 루프 재진입 (wait 중 break 걸렸을 수 있으므로)
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
                // 바빠지면 즉시 정지
                if ((phoneSwiper != null && phoneSwiper.isUsingTab) ||
                    (slideManager != null && !slideManager.isOK))
                {
                    moveTween.Kill();
                    break;
                }

                // 이동 중 충돌 체크 (이동하다가 멈춰있는 애랑 부딪힘)
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

    // 밀려나는 연출 (벽 튕기기 포함)
    private IEnumerator RepulsionMove(RectTransform obstacle)
    {
        // [수정] 서로 다른 부모를 가질 수 있으므로 World Position 기준으로 방향 계산
        Vector3 obsWorldPos = obstacle.position;
        Vector3 myWorldPos = rectTr.position;
        
        // 로컬 좌표도 벽 체크 등을 위해 필요함 (변수 복구)
        Vector3 myPos = rectTr.localPosition; 
        
        // 월드 기준 밀려날 방향
        Vector3 worldDir = (myWorldPos - obsWorldPos).normalized;

        // [안전장치] 위치가 완전히 겹쳐서 방향이 0이면 랜덤 방향으로 튐
        if (worldDir == Vector3.zero) worldDir = Random.insideUnitCircle.normalized;

        // 월드 방향을 내 로컬 방향으로 변환 (회전된 부모 대응)
        Vector3 localDir = worldDir;
        if (parentRect != null)
        {
            localDir = parentRect.InverseTransformDirection(worldDir); 
        }

        // 1차 목표 지점
        Vector3 target = myPos + localDir * pushDistance;

        // 벽 체크 (Boundary Check)
        RectTransform area = roamingArea != null ? roamingArea : parentRect;
        if (area != null)
        {
            // 영역을 로컬 좌표로 변환
            Vector3[] corners = new Vector3[4];
            area.GetWorldCorners(corners);
            Vector3 bl = parentRect.InverseTransformPoint(corners[0]);
            Vector3 tr = parentRect.InverseTransformPoint(corners[2]);

            // Min/Max 정확히 정렬
            float minX = Mathf.Min(bl.x, tr.x) + padding;
            float maxX = Mathf.Max(bl.x, tr.x) - padding;
            float minY = Mathf.Min(bl.y, tr.y) + padding;
            float maxY = Mathf.Max(bl.y, tr.y) - padding;
            
            // 만약 현재 위치 자체가 이미 벽 밖이라면? -> 안쪽으로 강제 복귀
            if (myPos.x < minX || myPos.x > maxX || myPos.y < minY || myPos.y > maxY)
            {
                 // 현재 위치가 범위 밖이면, 그냥 안쪽 랜덤한 곳으로 튀게 함
                 target = GetRandomLocalPosition(); 
                 // 혹은 가까운 벽 안쪽으로 설정
            }
            else
            {
                // 목표점이 벽 밖인가? (튕기기)
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
        return null; // 충돌 없음
    }

    // scale: 1.0f = 원래 크기, 0.5f = 절반 크기(중앙 기준)
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

    // ... GetRandomLocalPosition은 기존 유지 ...
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
