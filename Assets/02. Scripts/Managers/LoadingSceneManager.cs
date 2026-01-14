using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class LoadingSceneManager : MonoBehaviour
{
    [Header("프레임 애니메이터")]
    public LoadingBarUI loadingUI;

    [Header("퍼센트/텍스트(선택)")]
    public TMP_Text percentText;     // 없으면 비워두기
    public string nextSceneWhenLoaded; // 디버그용(직접 실행시)

    private static string _nextScene; // 정석 경로

    /// <summary>
    /// 외부에서 호출하는 정식 진입 메서드
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        _nextScene = sceneName;
        SceneManager.LoadScene("LoadingScene"); // 먼저 로딩씬 진입
    }

    // Start를 async void로 변경하여 비동기 메서드 실행 가능하게 함
    async void Start()
    {
        // 에디터에서 로딩씬만 단독 실행하는 경우 대비
        string target = string.IsNullOrEmpty(_nextScene) ? nextSceneWhenLoaded : _nextScene;

        //  StartCoroutine 제거 -> await 메서드 호출
        await LoadSceneAsync(target);
    }

    async UniTask LoadSceneAsync(string targetScene)
    {
        // 첫 프레임 대기 (초기화 안정성 확보)
        await UniTask.Yield();

        // 씬 비동기 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        float displayed = 0f;      // 화면에 보여줄 진행률(부드럽게 보간)
        float minShowTime = 0.8f;  // 로딩씬 최소 노출 시간(연출용)
        float elapsed = 0f;

        //  while 루프 구조는 유지하되 yield return null -> await UniTask.Yield()
        // op.progress가 0.9에서 멈추므로 isDone 대신 0.9 미만 체크나 루프 내부 로직으로 제어
        while (!op.isDone)
        {
            // 한 프레임 대기 (GC 할당 없음)
            await UniTask.Yield();

            elapsed += Time.deltaTime;

            // op.progress는 0.0~0.9가 로딩, 0.9~1.0은 활성화 단계
            float raw = Mathf.Clamp01(op.progress / 0.9f);

            // 보간하여 자연스럽게 증가
            displayed = Mathf.MoveTowards(displayed, raw, Time.deltaTime * 0.8f);

            loadingUI?.SetProgress(displayed);

            if (percentText != null)
            {
                int percent = Mathf.RoundToInt(displayed * 100f);
                percentText.text = $"{percent}%";
            }

            // 로딩 완료(0.99 이상) + 최소 노출 시간 충족 시 씬 활성화
            if (raw >= 1f && elapsed >= minShowTime)
            {
                // 마지막 프레임 시각적 완성
                loadingUI?.SetProgress(1f);
                if (percentText != null) percentText.text = "100%";

                // 씬 전환 허용
                op.allowSceneActivation = true;

                // 루프 종료
                break;
            }
        }
    }
}