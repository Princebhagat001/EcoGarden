using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class MouseClicker : MonoBehaviour
{
 [SerializeField]
 private Camera m_Camera;
 private bool mousePress = false;
 // This needs to be set to the Canvas raycaster. Just
 // drag the Canvas onto this variable in the inspector.
 [SerializeField]
 private GraphicRaycaster m_Raycaster;
 private PointerEventData m_PointerEventData;
 private EventSystem m_EventSystem;
 // Start is called before the first frame update
 void Start()
 {
 }
 // Update is called once per frame
 void Update()
 {
 Mouse mouse = Mouse.current;
 if (mouse.leftButton.wasPressedThisFrame)
 {
 mousePress = true;
 }
 }
 // Update is called once per frame
 void FixedUpdate()
 {
 if (mousePress)
 {
 mousePress = false;
 // Check if clicked on a gameObject.
 Mouse mouse = Mouse.current;
 Vector3 mousePosition = mouse.position.ReadValue();
 Ray ray = m_Camera.ScreenPointToRay(mousePosition);
 if (Physics.Raycast(ray, out RaycastHit hit))
 {
 Debug.Log("Clicked on: " + hit.collider.gameObject.name);
 IPointerClickHandler clickHandler =
 hit.collider.gameObject.GetComponent<IPointerClickHandler>();
 if (clickHandler != null)
 {
 PointerEventData pointerEventData =
 new PointerEventData(EventSystem.current);
 clickHandler.OnPointerClick(pointerEventData);
 }
 }
 // Check if clicked on UI.
 if (m_Raycaster)
 {
 m_PointerEventData = new PointerEventData(m_EventSystem);
 m_PointerEventData.position = Input.mousePosition;
 List<RaycastResult> results = new List<RaycastResult>();
 m_Raycaster.Raycast(m_PointerEventData, results);
 // Loops through all things clicked on, check if button and calls click.
 foreach (RaycastResult result in results)
 {
 Button aButton = result.gameObject.GetComponent<Button>();
if (aButton != null)
 {
 Debug.Log("Hit " + result.gameObject.name);
 Debug.Log("HitPosition " + result.worldPosition);
 aButton.onClick.Invoke();
 }
 }
 }
 }
 }
}