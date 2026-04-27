using System.Collections;
using UnityEngine;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Entities
{
    /// <summary>
    /// Plays a short spawn appearance by scaling the minion from 0 to 1.
    /// </summary>
    public class SummonedMinion : MonoBehaviour
{
    void Start()
    {
        // Play spawn appearance animation on creation
        StartCoroutine(AppearAnimation());
    }

    /// <summary>
    /// Smoothly scales the minion from zero to full size
    /// over a very short duration to simulate instant summoning.
    /// </summary>
    IEnumerator AppearAnimation()
    {
        transform.localScale = Vector3.zero;

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localScale = Vector3.one * (elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }
}

}
