using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// TextMeshProUGUI에 "로딩 중...", "유사성 계산 중..." 같은
/// 점(.) 애니메이션을 표시하는 컴포넌트
/// 
/// 사용법
/// 1. 이 스크립트를 TextMeshProUGUI가 붙은 오브젝트에 추가
/// 2. AlgorithmCall 등에서 loading.StartAnim("유사성 계산 중") 호출
/// 3. 완료 시 loading.StopAnim() 으로 중지
/// </summary>
public class UILoadingText : MonoBehaviour
{
    [Header("대상 텍스트 컴포넌트 (없으면 자동 탐색)")]
    [SerializeField] private TextMeshProUGUI target;

    [Header("기본 문구 (점이 붙을 문자열)")]
    [SerializeField] private string baseText = "로딩 중";

    [Header("점(.)이 갱신되는 간격 (초 단위)")]
    [SerializeField] private float interval = 0.4f;

    // 점 애니메이션용 문자열 배열
    private readonly string[] dots = { "", ".", "..", "..." };

    // 코루틴 실행 핸들 (중복 방지용)
    private Coroutine routine;

    // WaitForSeconds를 캐싱하여 GC 부담 최소화
    private WaitForSeconds waitCache;

    // 초기화 시, target이 비어 있으면 자동으로 찾고 WaitForSeconds 캐싱
    void Awake()
    {
        if (!target)
            target = GetComponent<TextMeshProUGUI>();

        waitCache = new WaitForSeconds(interval);
    }

    /// <summary>
    /// 로딩 애니메이션 시작
    /// </summary>
    /// <param name="overrideBaseText">문구를 변경하고 싶을 때 전달</param>
    public void StartAnim(string overrideBaseText = null)
    {
        // 문구를 일시적으로 교체 가능
        if (overrideBaseText != null)
            baseText = overrideBaseText;

        // 중복 실행 방지: 이전 코루틴 중지
        StopAnim();

        // 새로운 애니메이션 시작
        routine = StartCoroutine(Animate());
    }

    /// <summary>
    /// 현재 실행 중인 애니메이션을 정지
    /// </summary>
    public void StopAnim()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    /// <summary>
    /// "로딩 중..." 순환 표시
    /// </summary>
    private IEnumerator Animate()
    {
        int i = 0;

        while (true)
        {
            // 대상 텍스트가 존재하면 점(.) 개수에 맞춰 문구 갱신
            if (target)
                target.text = $"{baseText}{dots[i]}";

            // 다음 프레임 준비
            i = (i + 1) % dots.Length;

            // 지정된 간격만큼 대기
            yield return waitCache;
        }
    }
}
