using UnityEngine;
using TMPro;

public class History : MonoBehaviour
{
    public GameManager gamemanager;
    public RectTransform content;
    public GameObject HistoryViewPrafab;
    public void SetHistory() {

        // 스크롤 뷰 초기화
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        int sum = 0;

        //문자열을 가져와서 | 단위로 나누고
        string[] items = gamemanager.HistoryLIne.Split('|');

        foreach (string item in items) {

            if (string.IsNullOrWhiteSpace(item)) continue;

            // , 단위로 나눠서 각각 저장
            string[] parts = item.Split(',');

            string word = parts[0];
            string sim = parts[1];

            //프리랩을 만들고
            GameObject obj = Instantiate(HistoryViewPrafab, content);

            //문자열 적용
            TMP_Text wordText = obj.transform.Find("word").GetComponent<TMP_Text>();
            TMP_Text simText = obj.transform.Find("similarity").GetComponent<TMP_Text>();
            wordText.text = word;
            simText.text = sim;

            // 프리팹 위치 조정
            RectTransform rt = obj.GetComponent<RectTransform>();
            Vector2 pos = rt.anchoredPosition;
            pos.y = sum * -100f;
            rt.anchoredPosition = pos;

            // 스크롤 뷰 길이 조정
            Vector2 size = content.sizeDelta;
            size.y = 100f + (100f * sum);
            content.sizeDelta = size;

            sum++;
        }
    }
}
