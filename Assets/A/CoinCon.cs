using UnityEngine;

public class CoinCon : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(1);
            }

            Destroy(gameObject);
        }
    }
}