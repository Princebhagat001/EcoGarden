using UnityEngine;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Effects
{
    /// <summary>
    /// Expanding trigger that applies an impulse to nearby rigidbodies.
    /// Can optionally filter targets using a cone.
    /// </summary>
    public class Shockwave : MonoBehaviour
{
    [Header("Shockwave Settings")]

    /// <summary>
    /// Maximum expansion radius before the shockwave is destroyed.
    /// </summary>
    public float maxRadius = 5f;

    /// <summary>
    /// Speed at which the shockwave expands over time.
    /// </summary>
    public float expansionSpeed = 15f;

    /// <summary>
    /// Impulse force applied to affected rigidbodies.
    /// </summary>
    public float force = 20f;

    // ---------------- Cone filtering (optional) ----------------

    /// <summary>
    /// When true, the shockwave only applies force to objects inside the cone
    /// defined by coneOrigin, coneAngle and coneDistance.
    /// </summary>
    [Header("Optional Cone Filter")]
    public bool useCone = false;

    /// <summary>
    /// Transform that defines the cone origin and forward direction.
    /// </summary>
    public Transform coneOrigin = null;

    /// <summary>
    /// Full cone angle in degrees.
    /// </summary>
    [Tooltip("Full cone angle in degrees")]
    public float coneAngle = 60f;

    /// <summary>
    /// Maximum distance of the cone.
    /// </summary>
    public float coneDistance = 5f;

    /// <summary>
    /// Initial scale of the shockwave when spawned.
    /// </summary>
    private Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        // Expand the shockwave uniformly
        transform.localScale += Vector3.one * expansionSpeed * Time.deltaTime;

        // Destroy the shockwave once it reaches its maximum size
        if (transform.localScale.x >= maxRadius)
            Destroy(gameObject);
    }

    /// <summary>
    /// Applies an impulse force to any rigidbody entering the shockwave
    /// trigger collider. When useCone is true, only applies to objects
    /// inside the configured cone.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        if (useCone && coneOrigin != null)
        {
            Vector3 origin = coneOrigin.position;
            Vector3 forward = new Vector3(coneOrigin.forward.x, 0f, coneOrigin.forward.z).normalized;
            Vector3 toOther = other.transform.position - origin;
            toOther.y = 0f;
            float dist = toOther.magnitude;

            if (dist > 0.001f)
            {
                float ang = Vector3.Angle(forward, toOther.normalized);
                if (ang > (coneAngle * 0.5f) || dist > coneDistance)
                {
                    // outside cone -> ignore
                    return;
                }
            }
        }

        Vector3 dir = (other.transform.position - transform.position).normalized;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(dir * force, ForceMode.Impulse);
    }
}

}
