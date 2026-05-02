using System.Collections.Generic;
using UnityEngine;

public class HorseManager : MonoBehaviour
{
    private List<GameObject> Children = new List<GameObject>();
    private GOInteraction myGOI;

    [SerializeField] private GameObject interactionTextUI;
    private bool playerIsNear = false;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in transform)
        {
            if (child.tag == "Information")
            {
                Children.Add(child.gameObject);
            }
        }

        myGOI = GetComponent<GOInteraction>();
        if (myGOI == null)
        {
            Debug.Log("No GOInteraction attached to this object.");
        }

        // Default children to invisible.
        foreach (GameObject child in Children)
        {
            child.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (myGOI.Interaction == true)
        {
            foreach (GameObject child in Children)
            {
                if (child.activeSelf)
                {
                    child.SetActive(false);
                }
                else
                {
                    child.SetActive(true);
                }
            }

            // --- NEW: Hide the text when clicked ---
            if (interactionTextUI != null)
            {
                interactionTextUI.SetActive(false);
            }

            myGOI.Interaction = false;
        }
    }

    // Detect when player enters the animal's proximity collider
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered the trigger: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected!");
            playerIsNear = true;
            
            // --- NEW: Only show the prompt if the description isn't already active ---
            if (interactionTextUI != null && Children.Count > 0 && !Children[0].activeSelf)
            {
                interactionTextUI.SetActive(true);
            }
        }
    }

    // Hide text when the player walks away
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player walked away!");
            playerIsNear = false;
            if (interactionTextUI != null)
            {
                interactionTextUI.SetActive(false);
            }
        }
    }
}