using UnityEngine;
using UnityEngine.UI;

public class UIEffectController : MonoBehaviour
{
    public Image targetImage;

    public void SetGrayscale(bool isGray)
    {
        // 셰이더 그래프에서 만든 프로퍼티 이름(_GrayscaleAmount)을 사용
        float value = isGray ? 1f : 0f;
        targetImage.material.SetFloat("_GrayscaleAmount", value);
    }
}