using UnityEngine;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Core
{
    /// <summary>
    /// Data asset that stores Animator state names used by the viewer.
    /// One asset usually maps to one character.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationSet", menuName = "Animation Viewer/Animation Set")]
public class AnimationSet : ScriptableObject
{
    [Header("Optional Display Name")]
    /// <summary>
    /// Optional friendly name used to identify this animation set.
    /// Useful for UI labels, debugging, or editor organization.
    /// </summary>
    [Tooltip("Friendly name used for UI or debugging purposes.")]
    public string displayName;

    [Header("Animator Animation Names")]
    /// <summary>
    /// List of animation state names exactly as defined in the Animator.
    /// 
    /// These values are case-sensitive and must match the Animator states
    /// to ensure proper playback.
    /// </summary>
    [Tooltip("Exact animation state names as defined in the Animator.")]
    public string[] animationNames;

    [Header("Optional Effects")]
    /// <summary>
    /// Indicates whether this animation set triggers visual effects
    /// when animations change.
    /// 
    /// This flag is optional and can be used by external systems
    /// such as AnimationEffects.
    /// </summary>
    [Tooltip("Indicates whether this animation set uses visual effects.")]
    public bool hasEffects = false;
}

}
