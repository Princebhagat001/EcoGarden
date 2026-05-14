using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    public Transform player;
    public float interactDistance = 3f;

    public AudioSource audioSource;

    private bool playerNear = false;
    private bool isInteracting = false;

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Check if player is near
        playerNear = distance <= interactDistance;

        // Press E to interact
        if (playerNear && !isInteracting && Input.GetKeyDown(KeyCode.E))
        {
            if (audioSource != null)
            {
                isInteracting = true;
                audioSource.Play();
            }
        }

        // When audio finishes
        if (isInteracting && !audioSource.isPlaying)
        {
            isInteracting = false;
        }
    }

    void OnGUI()
    {
        // Show GUI only when:
        // player is near AND audio is not playing
        if (playerNear && !isInteracting)
        {
            GUIStyle style = new GUIStyle();

            style.fontSize = 30;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(
                new Rect(Screen.width / 2 - 100, Screen.height - 80, 200, 40),
                "Press E to interact",
                style
            );
        }
    }
}