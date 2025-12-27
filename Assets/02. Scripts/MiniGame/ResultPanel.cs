using TMPro;
using UnityEngine;

public class ResultPanel : MonoBehaviour
{
    public TextMeshProUGUI ModeText;
    public TextMeshProUGUI ScoreText;
    
    public void Init(bool isEasyMode, int clearCount)
    {
        if (ModeText) ModeText.text = $"{(isEasyMode ? "이지" : "하드")} 모드";
        if (ScoreText) ScoreText.text = $"{clearCount} 개의 미니게임 클리어!";
    }
}
