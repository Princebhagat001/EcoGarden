using TMPro;
using UnityEngine;
public class UIManager : MonoBehaviour
{
 [SerializeField]
 private TMP_Text InformationText;
 [SerializeField]
 private Canvas UICanvas;
 // Start is called before the first frame update
 void Start()
 {
 }
 // Update is called once per frame
 void Update()
 {
 }
 public void ToggleText()
 {
 if (InformationText)
 {
 if (InformationText.gameObject.activeSelf)
 {
 InformationText.gameObject.SetActive(false);
 }
 else
 {
 InformationText.gameObject.SetActive(true);
 }
 }
 }
 public void ToggleUI()
 {
 if (UICanvas)
 {
 if (UICanvas.gameObject.activeSelf)
 {
 UICanvas.gameObject.SetActive(false);
 }
 else
 {
 UICanvas.gameObject.SetActive(true);
 }
 }
 }
}