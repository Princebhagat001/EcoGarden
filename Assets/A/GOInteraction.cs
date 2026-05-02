using UnityEngine;

public class GOInteraction : MonoBehaviour
{
    private bool interaction = false;
    
    // Add text field to hold the custom interaction prompt
    public string promptText = "Click to see description";

    public bool Interaction
    {
        get { return interaction; }
        set { interaction = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }
}