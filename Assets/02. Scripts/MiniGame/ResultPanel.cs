using TMPro;
using UnityEngine;

public class ResultPanel : MonoBehaviour
{
    public TextMeshProUGUI ModeText;
    public TextMeshProUGUI ScoreText;
    
    public void Init(bool isEasyMode, int clearCount)
    {
        if (ModeText) ModeText.text = $"Mode : {(isEasyMode ? "Easy" : "Hard")} Mode";
        if (ScoreText) ScoreText.text = $"Cleared {clearCount} MiniGames";
    }
}
