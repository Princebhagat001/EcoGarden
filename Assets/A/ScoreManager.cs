using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private int score = 0;
    private string strScore = "Score: 0";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        score = Mathf.Clamp(score + amount, 0, 100);
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        strScore = "Score: " + score;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(20, 20, 200, 50), strScore, style);
    }
}