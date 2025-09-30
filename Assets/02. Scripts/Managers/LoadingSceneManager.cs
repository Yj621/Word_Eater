using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    void Start()
    {
        // 에디터에서 로딩씬만 단독 실행하는 경우 대비
        string target = string.IsNullOrEmpty(_nextScene) ? nextSceneWhenLoaded : _nextScene;
        StartCoroutine(LoadRoutine(target));
    }

    IEnumerator LoadRoutine(string targetScene)
    {
        yield return null;

        // 씬 비동기 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        float displayed = 0f;      // 화면에 보여줄 진행률(부드럽게 보간)
        float minShowTime = 0.8f;  // 로딩씬 최소 노출 시간(연출용)
        float elapsed = 0f;

        while (!op.isDone)
        {
            yield return null;
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
                // 마지막 프레임 고정
                loadingUI?.SetProgress(1f);
                if (percentText != null) percentText.text = "100%";
                op.allowSceneActivation = true;
            }
        }
    }
}