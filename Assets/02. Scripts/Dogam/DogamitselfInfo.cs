using UnityEngine;
using UnityEngine.UI;

public class DogamitselfInfo : MonoBehaviour
{
    public string sprid;
    public bool isOn = false; // 본 적 있는 애인지 확인하는 함수

    public void DogamOn() {
        if (isOn) {
            // 체인 이미지 숨기고
            Transform chain = transform.Find("Lock_Image");
            chain.gameObject.SetActive(false);

            // 메테리얼 제거
            Image img = GetComponent<Image>();
            img.material = null;
        }
    }

}
