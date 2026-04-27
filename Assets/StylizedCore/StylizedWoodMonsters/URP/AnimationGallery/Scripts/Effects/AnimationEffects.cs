using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.Controllers;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Effects
{
    /// <summary>
    /// Runs animation-driven effects like AoE impacts and summons.
    /// Effects are selected by animation name and spawned with the configured setup.
    /// </summary>
    public class AnimationEffects : MonoBehaviour
{
    /// <summary>
    /// Supported effect execution types.
    /// </summary>
    public enum EffectType
    {
        /// <summary>Area-of-effect impact with physics-based debris (radial).</summary>
        AoE,

        /// <summary>Summons one or more minion prefabs.</summary>
        Summon,

        /// <summary>
        /// Conical area-of-effect: uses the same visual debris prefab but
        /// only applies physics/impulses to debris inside a cone.
        /// </summary>
        ConeAoE
    }

    /// <summary>
    /// Configuration for one effect trigger.
    /// </summary>
    [System.Serializable]
    public class EffectSetup
    {
        /// <summary>
        /// Exact animation state name that triggers this effect.
        /// </summary>
        public string animationName;

        /// <summary>
        /// Type of effect to spawn (AoE, ConeAoE or Summon).
        /// </summary>
        public EffectType effectType;

        /// <summary>
        /// Main effect prefab.
        /// AoE / ConeAoE → impact debris prefab
        /// Summon → minion prefab
        /// </summary>
        public GameObject effectPrefab;

        /// <summary>
        /// Optional shockwave prefab used only for AoE effects.
        /// Can be the same prefab used for radial AoE; when used with
        /// ConeAoE we configure the Shockwave component to filter by cone.
        /// </summary>
        public GameObject shockwavePrefab;

        /// <summary>
        /// Transform used as the spawn origin for the effect.
        /// </summary>
        public Transform spawnPoint;

        /// <summary>
        /// Delay (in seconds) before spawning the effect.
        /// </summary>
        public float delay = 0f;

        /// <summary>
        /// Full cone angle in degrees (only used when effectType == ConeAoE).
        /// </summary>
        [Tooltip("Full cone angle in degrees (only for ConeAoE)")]
        public float coneAngle = 60f;

        /// <summary>
        /// Maximum distance (meters) of the cone (only used when effectType == ConeAoE).
        /// </summary>
        [Tooltip("Max distance of the cone (only for ConeAoE)")]
        public float coneDistance = 5f;

        /// <summary>
        /// Optional Euler rotation (degrees) applied to the instantiated effect prefab
        /// so the prefab's local forward can be corrected to match the spawnPoint forward.
        /// Example: if the prefab mesh faces -X, set this to (0,90,0) to rotate it to +Z.
        /// </summary>
        [Tooltip("Prefab forward correction in degrees (applied after LookRotation)")]
        public Vector3 prefabForwardEuler = Vector3.zero;
    }

    [Header("Effect Definitions")]
    public EffectSetup[] effectSetups;

    // ----------------------------------------------------------
    // AoE STATE
    // ----------------------------------------------------------

    /// <summary>Currently active AoE effect instance.</summary>
    private GameObject currentEffect;

    /// <summary>Rigidbodies used for AoE debris physics.</summary>
    private List<Rigidbody> cubeRBs = new();

    // Cone state (active when a ConeAoE was spawned)
    private bool currentEffectIsCone = false;
    private Transform currentEffectSpawnPoint = null;
    private float currentConeHalfAngle = 0f; // degrees (half-angle)
    private float currentConeDistance = 0f;
    // Cached horizontal forward used to evaluate cone direction (computed at spawn)
    private Vector3 currentEffectForwardHor = Vector3.forward;

    // ----------------------------------------------------------
    // SUMMON STATE
    // ----------------------------------------------------------

    /// <summary>Prevents multiple summon executions per animation loop.</summary>
    private bool summonUsed = false;

    /// <summary>List of currently spawned summoned units.</summary>
    private List<GameObject> spawnedMinions = new();

    /// <summary>Texture set controller of the summoning character.</summary>
    private TextureSetController invokerSet;

    void Awake()
    {
        invokerSet = GetComponentInChildren<TextureSetController>();

        if (invokerSet != null)
            invokerSet.OnSetChanged += UpdateSummonedMinionsSet;
    }

    /// <summary>
    /// Updates the texture set of all summoned minions when the invoker's
    /// texture set changes.
    /// </summary>
    void UpdateSummonedMinionsSet(int newSet)
    {
        foreach (var m in spawnedMinions)
        {
            if (m == null) continue;

            TextureSetController minionSet =
                m.GetComponentInChildren<TextureSetController>();

            if (minionSet != null)
                minionSet.ApplyExternalSet(newSet);
        }
    }

    void OnDestroy()
    {
        if (invokerSet != null)
            invokerSet.OnSetChanged -= UpdateSummonedMinionsSet;
    }

    // ----------------------------------------------------------
    // EFFECT EXECUTION
    // ----------------------------------------------------------

    /// <summary>
    /// Plays the effect associated with a given animation name.
    /// Intended to be called from animation events or animation
    /// change handlers.
    /// </summary>
    public void PlayEffect(string animName)
    {
        foreach (var setup in effectSetups)
        {
            if (setup.animationName != animName)
                continue;

            if (setup.effectType == EffectType.AoE)
                SpawnAoE(setup);
            else if (setup.effectType == EffectType.ConeAoE)
                SpawnConeAoE(setup);
            else if (setup.effectType == EffectType.Summon)
                StartCoroutine(SpawnSummons(setup));
        }
    }

    // ----------------------------------------------------------
    // SUMMON LOGIC
    // ----------------------------------------------------------

    /// <summary>
    /// Spawns multiple summoned units in front of the invoker,
    /// applying correct orientation and texture synchronization.
    /// </summary>
    IEnumerator SpawnSummons(EffectSetup setup)
    {
        if (summonUsed)
            yield break;

        summonUsed = true;

        if (setup.delay > 0)
            yield return new WaitForSeconds(setup.delay);

        if (setup.effectPrefab == null)
        {
            Debug.LogWarning("Summon aborted: missing minion prefab.");
            yield break;
        }

        CleanupSummons();

        Transform c = setup.spawnPoint;

        Vector3 f = new Vector3(c.forward.x, 0, c.forward.z).normalized;
        Vector3 r = new Vector3(c.right.x, 0, c.right.z).normalized;

        float y = c.position.y;

        Vector3 posCenter = c.position + f * 2.0f; posCenter.y = y;
        Vector3 posLeft = c.position + f * 1.3f - r * 1.8f; posLeft.y = y;
        Vector3 posRight = c.position + f * 1.3f + r * 1.8f; posRight.y = y;

        Quaternion rot = Quaternion.LookRotation(f);

        spawnedMinions.Add(Instantiate(setup.effectPrefab, posCenter, rot, transform));
        spawnedMinions.Add(Instantiate(setup.effectPrefab, posLeft, rot, transform));
        spawnedMinions.Add(Instantiate(setup.effectPrefab, posRight, rot, transform));

        foreach (var m in spawnedMinions)
            ApplyInvokerSet(m);
    }

    /// <summary>
    /// Applies the invoker's current texture set to a summoned unit.
    /// </summary>
    void ApplyInvokerSet(GameObject minion)
    {
        TextureSetController invoker =
            GetComponentInChildren<TextureSetController>();

        TextureSetController minionSet =
            minion.GetComponentInChildren<TextureSetController>();

        if (invoker != null && minionSet != null)
            minionSet.ApplyExternalSet(invoker.CurrentSet);
    }

    /// <summary>
    /// Resets the summon state and allows the animation to summon again.
    /// Intended to be called from animation frame 0 events.
    /// </summary>
    public void ResetSummon()
    {
        CleanupSummons();
        summonUsed = false;
    }

    // ----------------------------------------------------------
    // AOE LOGIC
    // ----------------------------------------------------------

    /// <summary>
    /// Spawns an area-of-effect impact and prepares debris physics.
    /// </summary>
    void SpawnAoE(EffectSetup setup)
    {
        CleanupAoE();

        if (setup.effectPrefab == null || setup.spawnPoint == null)
            return;

        // compute horizontal forward from the spawn point to avoid pitch/roll issues
        Vector3 forwardHor = new Vector3(setup.spawnPoint.forward.x, 0f, setup.spawnPoint.forward.z).normalized;
        if (forwardHor.sqrMagnitude < 0.0001f) forwardHor = Vector3.forward;

        currentEffectForwardHor = forwardHor;
        currentEffectIsCone = false;
        currentEffectSpawnPoint = setup.spawnPoint;
        currentConeHalfAngle = 0f;
        currentConeDistance = 0f;

        currentEffect = Instantiate(
            setup.effectPrefab,
            setup.spawnPoint.position,
            Quaternion.LookRotation(forwardHor) * Quaternion.Euler(setup.prefabForwardEuler),
            transform
        );

        cubeRBs.Clear();

        foreach (Rigidbody rb in currentEffect.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;

            Collider col = rb.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            cubeRBs.Add(rb);
        }
    }

    /// <summary>
    /// Spawns an area-of-effect impact prepared to affect only a cone region.
    /// We reuse the same prefab layout (debris) but record cone params for LaunchCubes().
    /// </summary>
    void SpawnConeAoE(EffectSetup setup)
    {
        CleanupAoE();

        if (setup.effectPrefab == null || setup.spawnPoint == null)
            return;

        // compute horizontal forward from the spawn point to avoid pitch/roll issues
        Vector3 forwardHor = new Vector3(setup.spawnPoint.forward.x, 0f, setup.spawnPoint.forward.z).normalized;
        if (forwardHor.sqrMagnitude < 0.0001f) forwardHor = Vector3.forward;

        currentEffectForwardHor = forwardHor;
        currentEffectIsCone = true;
        currentEffectSpawnPoint = setup.spawnPoint;
        currentConeHalfAngle = Mathf.Max(0.1f, setup.coneAngle * 0.5f);
        currentConeDistance = Mathf.Max(0.1f, setup.coneDistance);

        currentEffect = Instantiate(
            setup.effectPrefab,
            setup.spawnPoint.position,
            Quaternion.LookRotation(forwardHor) * Quaternion.Euler(setup.prefabForwardEuler),
            transform
        );

        cubeRBs.Clear();

        foreach (Rigidbody rb in currentEffect.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;

            Collider col = rb.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            cubeRBs.Add(rb);
        }

    }

    /// <summary>
    /// Activates physics on AoE debris and optionally spawns a shockwave.
    /// For ConeAoE only cubes inside the defined cone are affected.
    /// </summary>
    public void LaunchCubes()
    {
        if (currentEffect == null)
            return;

        // Try to spawn shockwave: prefer setup matching the active spawnPoint (AoE or ConeAoE)
        if (effectSetups != null && currentEffectSpawnPoint != null)
        {
            bool spawned = false;
            foreach (var setup in effectSetups)
            {
                if (setup == null || setup.spawnPoint == null || setup.shockwavePrefab == null)
                    continue;

                if (setup.spawnPoint == currentEffectSpawnPoint)
                {
                    var swObj = Instantiate(setup.shockwavePrefab, setup.spawnPoint.position, Quaternion.identity);
                    var sw = swObj.GetComponent<Shockwave>();
                    if (sw != null && setup.effectType == EffectType.ConeAoE)
                    {
                        sw.useCone = true;
                        sw.coneOrigin = setup.spawnPoint;
                        sw.coneAngle = setup.coneAngle;
                        sw.coneDistance = setup.coneDistance;
                    }
                    spawned = true;
                    break;
                }
            }

            if (!spawned)
            {
                // fallback: spawn first radial AoE shockwave if available
                foreach (var setup in effectSetups)
                {
                    if (setup == null) continue;
                    if (setup.effectType == EffectType.AoE && setup.shockwavePrefab != null && setup.spawnPoint != null)
                    {
                        Instantiate(setup.shockwavePrefab, setup.spawnPoint.position, Quaternion.identity);
                        break;
                    }
                }
            }
        }

        if (currentEffectIsCone && currentEffectSpawnPoint != null)
        {
            // Cone-specific behavior: only enable physics for debris inside cone
            Vector3 origin = currentEffectSpawnPoint.position;
            // use cached horizontal forward computed at spawn time
            Vector3 forwardHor = currentEffectForwardHor;

            float baseForce = 8f;

            int applied = 0;

            foreach (var rb in cubeRBs)
            {
                if (rb == null) continue;

                Vector3 toRb = rb.transform.position - origin;
                toRb.y = 0f;
                float dist = toRb.magnitude;

                bool eligible = false;
                if (dist <= 0.001f)
                {
                    eligible = true;
                }
                else
                {
                    float ang = Vector3.Angle(forwardHor, toRb.normalized);
                    eligible = (ang <= currentConeHalfAngle) && (dist <= currentConeDistance);
                }

                if (!eligible)
                {
                    // leave as kinematic and collider disabled
                    continue;
                }

                // enable physics on eligible Rigidbodies
                rb.isKinematic = false;

                Collider col = rb.GetComponent<Collider>();
                if (col != null) col.enabled = true;

                // apply an outward impulse from the spawn origin with some variation
                Vector3 impulseOrigin = origin;
                Vector3 dir = (rb.transform.position - impulseOrigin).normalized;
                dir.y = 0.35f + Random.Range(0f, 0.2f);
                dir.Normalize();

                float attenuation = 1f;
                if (currentConeDistance > 0f)
                    attenuation = Mathf.Clamp01(1f - (dist / currentConeDistance));

                rb.AddForce(dir * baseForce * (0.6f + attenuation), ForceMode.Impulse);
                applied++;
            }

            Debug.Log($"LaunchCubes (cone): applied physics to {applied}/{cubeRBs.Count} debris");

            // destroy effect root after some time to free objects
            Destroy(currentEffect, 4f);
            // keep references so CleanupAoE() can immediately destroy remaining debris when animation changes
            currentEffectIsCone = false;
            currentEffectSpawnPoint = null;
            currentConeHalfAngle = 0f;
            currentConeDistance = 0f;

            return;
        }

        // Default radial behavior: enable physics for all debris
        foreach (var rb in cubeRBs)
        {
            if (rb == null) continue;

            rb.isKinematic = false;

            Collider col = rb.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        Destroy(currentEffect, 4f);
        // keep references until CleanupAoE or scheduled destroy runs
     }

    // ----------------------------------------------------------
    // CLEANUP
    // ----------------------------------------------------------

    /// <summary>
    /// Removes the active AoE effect and resets internal AoE/Cone state.
    /// </summary>
    void CleanupAoE()
    {
        // Destroy the effect root if present
        if (currentEffect != null)
            Destroy(currentEffect);

        // Also destroy any debris Rigidbodies that may have been detached from the effect root
        if (cubeRBs != null)
        {
            for (int i = 0; i < cubeRBs.Count; i++)
            {
                var rb = cubeRBs[i];
                if (rb == null) continue;
                // Destroy the whole debris GameObject to ensure it is removed immediately
                Destroy(rb.gameObject);
            }
            cubeRBs.Clear();
        }

        currentEffect = null;
        currentEffectIsCone = false;
        currentEffectSpawnPoint = null;
        currentConeHalfAngle = 0f;
        currentConeDistance = 0f;
        currentEffectForwardHor = Vector3.forward;
    }

    /// <summary>
    /// Destroys all currently spawned summoned units.
    /// </summary>
    void CleanupSummons()
    {
        foreach (var m in spawnedMinions)
        {
            if (m != null)
                Destroy(m);
        }
        spawnedMinions.Clear();
    }

    /// <summary>
    /// Called when the animation changes to ensure all effects
    /// are properly reset.
    /// </summary>
    public void OnAnimationChanged()
    {
        StopAllCoroutines();
        CleanupAoE();
        ResetSummon();
    }

    /// <summary>
    /// Draw editor gizmos to visualize configured cones for ConeAoE setups
    /// and the currently active cone when spawned.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw configured cones from the effectSetups so designers can tune angle/distance
        if (effectSetups != null)
        {
            foreach (var setup in effectSetups)
            {
                if (setup == null) continue;
                if (setup.effectType != EffectType.ConeAoE) continue;
                if (setup.spawnPoint == null) continue;

                // compute horizontal forward for gizmo (same method used at spawn)
                Vector3 cfgForwardHor = new Vector3(setup.spawnPoint.forward.x, 0f, setup.spawnPoint.forward.z).normalized;
                if (cfgForwardHor.sqrMagnitude < 0.0001f) cfgForwardHor = Vector3.forward;

                DrawConeGizmo(setup.spawnPoint.position, cfgForwardHor, setup.coneAngle * 0.5f, setup.coneDistance, new Color(0f, 0.5f, 1f, 0.15f));
            }
        }

        // Draw currently active cone (if any) with a stronger color
        if (currentEffectIsCone && currentEffectSpawnPoint != null)
        {
            // use cached horizontal forward computed at spawn time to ensure gizmo matches actual cone logic
            DrawConeGizmo(currentEffectSpawnPoint.position, currentEffectForwardHor, currentConeHalfAngle, currentConeDistance, new Color(1f, 0.3f, 0.0f, 0.25f));
        }
    }

    // Helper: draw a wire-outline cone on the XZ plane plus filled-ish lines
    private void DrawConeGizmo(Vector3 origin, Vector3 forward, float halfAngleDeg, float distance, Color color)
    {
        // project forward to horizontal
        Vector3 fHor = new Vector3(forward.x, 0f, forward.z).normalized;
        if (fHor.sqrMagnitude < 0.0001f) fHor = Vector3.forward;

        // Save
        Color prev = Gizmos.color;

        // Draw semi-transparent fan by drawing many lines
        int segments = 36;
        float startAngle = -halfAngleDeg;
        float endAngle = halfAngleDeg;
        float step = (endAngle - startAngle) / segments;

        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float a = startAngle + step * i;
            Quaternion rot = Quaternion.AngleAxis(a, Vector3.up);
            Vector3 dir = rot * fHor;
            Vector3 point = origin + dir.normalized * distance;

            if (i > 0)
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.25f);
                Gizmos.DrawLine(prevPoint, point);
            }

            // radial lines
            Gizmos.color = new Color(color.r, color.g, color.b, 0.08f);
            Gizmos.DrawLine(origin, point);

            prevPoint = point;
        }

        // Draw outline in stronger color
        Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
        Vector3 leftDir = Quaternion.AngleAxis(-halfAngleDeg, Vector3.up) * fHor;
        Vector3 rightDir = Quaternion.AngleAxis(halfAngleDeg, Vector3.up) * fHor;
        Gizmos.DrawLine(origin, origin + leftDir.normalized * distance);
        Gizmos.DrawLine(origin, origin + rightDir.normalized * distance);
        // arc
        int arcSegments = 18;
        Vector3 prevArc = origin + leftDir.normalized * distance;
        for (int i = 1; i <= arcSegments; i++)
        {
            float a = -halfAngleDeg + (i * (halfAngleDeg * 2f) / arcSegments);
            Quaternion rot = Quaternion.AngleAxis(a, Vector3.up);
            Vector3 p = origin + (rot * fHor).normalized * distance;
            Gizmos.DrawLine(prevArc, p);
            prevArc = p;
        }

        // small origin marker
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 1f);
        Gizmos.DrawSphere(origin, 0.05f);

        Gizmos.color = prev;
    }
}

}
