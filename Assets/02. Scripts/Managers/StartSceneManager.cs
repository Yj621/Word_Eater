using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [Header("움직일 텍스트")]
    public TextMeshProUGUI startText;
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
        LoadingSceneManager.LoadScene("WordEater");
    }
}
