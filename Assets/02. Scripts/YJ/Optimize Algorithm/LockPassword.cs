using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LockPassword : MonoBehaviour
{
    [SerializeField] private Image dotImage;   // 잠김 상태일 때 보일 점 이미지
    [SerializeField] private TMP_Text charText; // 힌트(초성) 텍스트

    [Range(0f, 1f)][SerializeField] private float inactiveAlpha = 0.25f; // 비활성 느낌 줄 때 투명도

    /// <summary>
    /// 점(●) 모드로 설정하는 함수
    /// </summary>
    public void SetDot(bool active = true)
    {
        dotImage.enabled = true; // 점 이미지 켬

        // 활성/비활성 여부에 따라 투명도 조절해서 켜진 느낌/꺼진 느낌 줌
        var c = dotImage.color;
        c.a = active ? 1f : inactiveAlpha;
        dotImage.color = c;

        // 글자는 가리고 내용 비움
        charText.gameObject.SetActive(false);
        charText.text = "";
    }

    /// <summary>
    /// 특정 문자(초성)를 보여주는 모드로 설정하는 함수
    /// </summary>
    public void SetChar(string s)
    {
        dotImage.enabled = false; // 점 이미지는 끔

        // 텍스트 오브젝트 켜고 글자 넣어줌
        charText.gameObject.SetActive(true);
        charText.text = s;
    }
}