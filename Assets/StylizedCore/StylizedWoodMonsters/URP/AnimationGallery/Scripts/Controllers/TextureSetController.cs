using UnityEngine;
using System.Collections.Generic;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Controllers
{
    /// <summary>
    /// Applies and switches texture sets across all renderers of a model,
    /// including LOD renderers.
    /// </summary>
    public class TextureSetController : MonoBehaviour
{
    /// <summary>
    /// Texture bundle used by one skin variation.
    /// </summary>
    [System.Serializable]
    public class TextureSet
    {
        public Texture2D albedo;
        public Texture2D normal;
        public Texture2D metallicSmoothness;
        public Texture2D maskMap;
        public Texture2D ao;
        public Texture2D emission;
        public Texture2D height;
    }

    /// <summary>
    /// All available texture sets for this model.
    /// </summary>
    public TextureSet[] sets;

    /// <summary>
    /// Index of the currently active texture set.
    /// </summary>
    private int currentSet = 0;

    /// <summary>
    /// Runtime instances of all materials used by all renderers (including LODs).
    /// </summary>
    private List<Material> runtimeMaterials = new List<Material>();

    /// <summary>
    /// Returns the index of the current texture set.
    /// </summary>
    public int CurrentSet => currentSet;

    /// <summary>
    /// Returns the total number of available texture sets.
    /// </summary>
    public int TotalSets => sets.Length;

    /// <summary>
    /// Event triggered whenever the active texture set changes.
    /// </summary>
    public System.Action<int> OnSetChanged;

    /// <summary>
    /// Initializes the runtime material instance.
    /// </summary>
    void Start()
    {
        InitializeRuntimeMaterials();
    }

    /// <summary>
    /// Advances to the next texture set in sequence.
    /// </summary>
    public void NextSet()
    {
        currentSet = (currentSet + 1) % sets.Length;
        ApplySet(currentSet);
    }

    /// <summary>
    /// Returns to the previous texture set in sequence.
    /// </summary>
    public void PreviousSet()
    {
        currentSet = (currentSet - 1 + sets.Length) % sets.Length;
        ApplySet(currentSet);
    }

    /// <summary>
    /// Resets the texture set to the first one (index 0).
    /// </summary>
    public void ResetSet()
    {
        currentSet = 0;
        ApplySet(0);
    }

    /// <summary>
    /// Applies a specific texture set to the runtime material.
    /// </summary>
    /// <param name="index">Index of the texture set to apply.</param>
    void ApplySet(int index)
    {
        // Rebuild list each apply to include all active/inactive LOD renderers/material instances
        InitializeRuntimeMaterials();
        if (runtimeMaterials == null || runtimeMaterials.Count == 0) return;

        if (sets == null || sets.Length == 0)
            return;

        var s = sets[index];

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            Material runtimeMaterial = runtimeMaterials[i];
            if (runtimeMaterial == null) continue;

            // ALBEDO
            SetFirstTexture(runtimeMaterial, s.albedo, "_BaseColorMap", "_BaseMap", "_MainTex");

            // NORMAL MAP
            if (s.normal != null)
            {
                bool normalApplied = SetFirstTexture(runtimeMaterial, s.normal, "_NormalMap", "_BumpMap");
                if (normalApplied)
                {
                    runtimeMaterial.EnableKeyword("_NORMALMAP");
                    runtimeMaterial.EnableKeyword("_NORMAL_MAP");
                }
            }
            else
            {
                runtimeMaterial.DisableKeyword("_NORMALMAP");
                runtimeMaterial.DisableKeyword("_NORMAL_MAP");
            }

            // METALLIC / SMOOTHNESS (Built-in/URP) + MASK MAP (HDRP)
            bool metallicApplied = SetFirstTexture(runtimeMaterial, s.metallicSmoothness, "_MetallicGlossMap");
            Texture hdrpMask = s.maskMap != null ? s.maskMap : s.metallicSmoothness;
            bool maskApplied = SetFirstTexture(runtimeMaterial, hdrpMask, "_MaskMap");
            if (metallicApplied)
            {
                runtimeMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
                runtimeMaterial.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                runtimeMaterial.DisableKeyword("_METALLICSPECGLOSSMAP");
                runtimeMaterial.DisableKeyword("_METALLICGLOSSMAP");
            }

            if (maskApplied && hdrpMask != null)
            {
                runtimeMaterial.EnableKeyword("_MASKMAP");
            }
            else
            {
                runtimeMaterial.DisableKeyword("_MASKMAP");
            }

            // AMBIENT OCCLUSION
            SetFirstTexture(runtimeMaterial, s.ao, "_AmbientOcclusionTexture", "_OcclusionMap");

            // EMISSION
            if (s.emission != null)
            {
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.EnableKeyword("_EMISSIVE_COLOR_MAP");
                SetFirstTexture(runtimeMaterial, s.emission, "_EmissiveColorMap", "_EmissionMap");
            }
            else
            {
                runtimeMaterial.DisableKeyword("_EMISSION");
                runtimeMaterial.DisableKeyword("_EMISSIVE_COLOR_MAP");
            }

            // HEIGHT / PARALLAX
            if (s.height != null)
            {
                runtimeMaterial.EnableKeyword("_PARALLAXMAP");
                SetFirstTexture(runtimeMaterial, s.height, "_HeightMap", "_ParallaxMap");
            }
            else
            {
                runtimeMaterial.DisableKeyword("_PARALLAXMAP");
            }
        }

        // Notify listeners
        OnSetChanged?.Invoke(currentSet);
    }

    /// <summary>
    /// Applies a texture set from an external caller using a safe index.
    /// </summary>
    /// <param name="index">Requested texture set index.</param>
    public void ApplyExternalSet(int index)
    {
        currentSet = Mathf.Clamp(index, 0, sets.Length - 1);

        if (runtimeMaterials == null || runtimeMaterials.Count == 0)
        {
            InitializeRuntimeMaterials();
        }

        ApplySet(currentSet);
    }

    /// <summary>
    /// Forces the material to use a specific texture set index.
    /// Intended for synchronized or group-based skin changes.
    /// </summary>
    /// <param name="index">Texture set index to apply.</param>
    public void SetTextureSet(int index)
    {
        // Ensure index stays within valid range
        currentSet = index % sets.Length;
        ApplySet(currentSet);
    }

    /// <summary>
    /// Rebuilds the list of runtime material instances from child renderers
    /// and the nearest parent LODGroup renderers.
    /// </summary>
    private void InitializeRuntimeMaterials()
    {
        if (runtimeMaterials == null)
            runtimeMaterials = new List<Material>();

        runtimeMaterials.Clear();
        HashSet<Renderer> collectedRenderers = new HashSet<Renderer>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (!collectedRenderers.Add(renderers[i])) continue;

            var rendererMaterials = renderers[i].materials;
            for (int j = 0; j < rendererMaterials.Length; j++)
            {
                var mat = rendererMaterials[j];
                if (mat != null)
                    runtimeMaterials.Add(mat);
            }
        }

        // Include renderers explicitly assigned to the nearest parent LODGroup.
        // Using only the nearest one prevents cross-updating parent/child characters
        // (e.g. summoner and summoned minions).
        LODGroup nearestLodGroup = GetComponentInParent<LODGroup>();
        if (nearestLodGroup != null)
        {
            var lods = nearestLodGroup.GetLODs();
            for (int l = 0; l < lods.Length; l++)
            {
                var lodRenderers = lods[l].renderers;
                for (int r = 0; r < lodRenderers.Length; r++)
                {
                    var lodRenderer = lodRenderers[r];
                    if (lodRenderer == null) continue;
                    if (!collectedRenderers.Add(lodRenderer)) continue;

                    var lodRendererMaterials = lodRenderer.materials;
                    for (int m = 0; m < lodRendererMaterials.Length; m++)
                    {
                        var mat = lodRendererMaterials[m];
                        if (mat != null)
                            runtimeMaterials.Add(mat);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sets a texture on the first material property found in the provided list.
    /// Returns true when a matching property is found.
    /// </summary>
    private bool SetFirstTexture(Material material, Texture texture, params string[] propertyNames)
    {
        if (material == null || propertyNames == null)
            return false;

        for (int i = 0; i < propertyNames.Length; i++)
        {
            string prop = propertyNames[i];
            if (material.HasProperty(prop))
            {
                material.SetTexture(prop, texture);
                return true;
            }
        }

        return false;
    }
}

}
