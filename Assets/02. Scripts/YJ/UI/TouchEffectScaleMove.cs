using UnityEngine;

public class TouchEffectScaleMove : MonoBehaviour
{
    public float minScale = 0.8f;   // 최소 크기
    public float maxScale = 1.2f;   // 최대 크기
    public float speed = 2f;

    public RectTransform rectTransform;
    private bool isActive = false;

    void OnEnable()
    {
        isActive = true; // 이미지가 활성화될 때 애니메이션 시작
    }

    void OnDisable()
    {
        isActive = false; // 이미지가 비활성화될 때 애니메이션 멈춤
        rectTransform.localScale = Vector3.one; // 원래 크기로 초기화
    }

    void Update()
    {
        if (!isActive) return;

        // Time.time를 이용해서 Ping-Pong 스케일
        float scale = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(Time.time * speed, 1f));
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }
}
