using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [Header("움직일 텍스트")]
    public TextMeshProUGUI startText;
    [Header("페이드 그룹(옵션)")]
    public CanvasGroup promptCanvasGroup;

    [Header("스토리 씬 이름")]
    public string storySceneName = "StoryScene";

    void Start()
    {
        if (startText != null)
        {
            // anchoredPosition.y를 기준으로 위아래로 움직이게 함
            RectTransform rt = startText.GetComponent<RectTransform>();
            rt.DOAnchorPosY(rt.anchoredPosition.y + 20f, 1f) // 위로 20
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo); // 무한 반복 (왔다갔다)
        }
    }

    public void MainScene()
    {
        // 첫 진입(스토리 미시청) 체크
        int hasWatch = PlayerPrefs.GetInt("HasWatchStory", 0);

        if (hasWatch == 0)
        {
            // 스토리 씬으로 이동
            // 페이드 효과가 있으면 재생 후 이동
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    SceneManager.LoadScene(storySceneName);
                });
            }
            else
            {
                SceneManager.LoadScene(storySceneName);
            }
        }
        else
        {
            // 이미 봤으면 바로 로딩 -> 메인
            LoadingSceneManager.LoadScene("WordEater");
        }
    }
}
