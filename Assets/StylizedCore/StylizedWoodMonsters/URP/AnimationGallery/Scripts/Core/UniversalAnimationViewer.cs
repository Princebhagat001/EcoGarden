using UnityEngine;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.Controllers;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.Effects;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.CameraControllers;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Core
{
    /// <summary>
    /// Viewer display modes.
    /// </summary>
    public enum ViewerMode { Simple, Advanced }

/// <summary>
/// Main runtime viewer for animations, skin switching and camera integration.
/// </summary>
public class UniversalAnimationViewer : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationEffects animationEffects;
    [SerializeField] private TextureSetController textureSetController;
    [SerializeField] private ViewerCameraController cameraController;

    [Header("CAMERA SETTINGS")]
    /// <summary>
    /// Manual height value used by the camera to adapt to this model.
    /// </summary>
    public float alturaManual = 1.8f;

    /// <summary>
    /// Custom zoom value applied when the camera focuses this model.
    /// </summary>
    public float zoomPersonalizado = 1.8f;

    [Header("CONFIGURATION")]
    /// <summary>
    /// Current viewer mode.
    /// </summary>
    public ViewerMode mode = ViewerMode.Advanced;

    /// <summary>
    /// Animation set containing the exact Animator state names.
    /// </summary>
    public AnimationSet animationSet;

    private bool paused = false;
    private bool showUI = true;
    private bool minimalUI = false;
    private int currentIndex = 0;
    private string currentAnimation = "";

    /// <summary>
    /// Initializes references, validates configuration
    /// and forces the initial camera adaptation.
    /// </summary>
    private void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animationSet == null || animationSet.animationNames.Length == 0)
        {
            Debug.LogError("❌ UniversalAnimationViewer: No AnimationSet assigned.");
            enabled = false;
            return;
        }

        if (textureSetController == null)
            textureSetController = GetComponentInChildren<TextureSetController>();

        if (animationEffects == null)
            animationEffects = GetComponentInChildren<AnimationEffects>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<ViewerCameraController>();

        // Force initial camera adaptation
        if (cameraController != null)
        {
            cameraController.SetTarget(this.transform);
            // Ensures camera framing is correct from frame 1
            cameraController.AdaptToModel(alturaManual, zoomPersonalizado);
        }

        PlayAnimation(0);
    }

    /// <summary>
    /// Updates input handling and manages UI visibility
    /// based on camera presentation state.
    /// </summary>
    private void Update()
    {
        HandleInput();

        if (cameraController != null)
        {
            // 1. If the camera is in PRESENTATION mode, UI is always hidden.
            if (cameraController.IsPresenting)
            {
                showUI = false;
                ViewerCameraController.UIHiddenGlobal = false;
            }
            // 2. Otherwise, UI visibility depends on the global H toggle.
            else
            {
                showUI = !ViewerCameraController.UIHiddenGlobal;
            }
        }
    }

    /// <summary>
    /// Handles keyboard input for animation playback,
    /// pause control and texture set switching.
    /// </summary>
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A)) PreviousAnimation();
        if (Input.GetKeyDown(KeyCode.D)) NextAnimation();
        if (Input.GetKeyDown(KeyCode.Space)) RestartCurrentAnimation();

        if (Input.GetKeyDown(KeyCode.P))
        {
            paused = !paused;
            if (animator != null) animator.speed = paused ? 0f : 1f;
        }

        if (textureSetController != null)
        {
            if (Input.GetKeyDown(KeyCode.Z)) textureSetController.PreviousSet();
            if (Input.GetKeyDown(KeyCode.X)) textureSetController.NextSet();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            minimalUI = !minimalUI;
        }

    }

    /// <summary>
    /// Plays an animation from the AnimationSet by index.
    /// </summary>
    /// <param name="index">Index of the animation to play.</param>
    public void PlayAnimation(int index)
    {
        if (index < 0 || index >= animationSet.animationNames.Length) return;
        if (animationEffects != null) animationEffects.OnAnimationChanged();

        animator.Rebind();
        animator.Update(0f);
        currentIndex = index;
        currentAnimation = animationSet.animationNames[index];
        animator.Play(currentAnimation, 0, 0f);
    }

    /// <summary>
    /// Plays the next animation in the list.
    /// </summary>
    public void NextAnimation() =>
        PlayAnimation((currentIndex + 1) % animationSet.animationNames.Length);

    /// <summary>
    /// Plays the previous animation in the list.
    /// </summary>
    public void PreviousAnimation() =>
        PlayAnimation((currentIndex - 1 + animationSet.animationNames.Length) % animationSet.animationNames.Length);

    /// <summary>
    /// Restarts the currently playing animation.
    /// </summary>
    public void RestartCurrentAnimation() =>
        animator.Play(currentAnimation, 0, 0f);

    /// <summary>
    /// Draws the on-screen UI with animation, camera
    /// and texture set controls. Optimized for 1080p scaling.
    /// </summary>
    void OnGUI()
    {
        if (!showUI) return;

        // Resolution-independent scaling matrix
        float scale = Screen.height / 1080f;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float y = 15f;
        float leftMargin = 30f;
        float rightMargin = 20f;

        // --- STYLES ---
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };
        GUIStyle modelNameStyle = new GUIStyle(titleStyle)
        {
            fontSize = 32,
            normal = { textColor = new Color(0.2f, 0.8f, 1f) }
        };
        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            normal = { textColor = Color.white },
            richText = true
        };

        // --- ALWAYS VISIBLE ELEMENTS (Titles) ---
        // Animation State Title (Left)
        GUI.Label(new Rect(leftMargin + 2, y + 2, 900, 55), $"▶ {currentAnimation}", new GUIStyle(titleStyle) { normal = { textColor = Color.black } });
        GUI.Label(new Rect(leftMargin, y, 900, 55), $"▶ {currentAnimation}", titleStyle);

        // Model Information (Right)
        if (cameraController != null && cameraController.GetTarget() != null)
        {
            float virtualWidth = Screen.width / scale;
            float width = 500f;
            float x = virtualWidth - width - rightMargin;
            string modelName = cameraController.GetTarget().name;

            // Model Name with Shadow
            GUI.Label(new Rect(x + 2, 10, width, 55), modelName, new GUIStyle(modelNameStyle) { normal = { textColor = Color.black }, alignment = TextAnchor.UpperRight });
            GUI.Label(new Rect(x, 8, width, 55), modelName, new GUIStyle(modelNameStyle) { alignment = TextAnchor.UpperRight });

            // --- SKIN CONTROLS (Right side, hidden in Minimal Mode) ---
            if (!minimalUI && textureSetController != null)
            {
                string setText = $"Skin: {textureSetController.CurrentSet + 1} / {textureSetController.TotalSets}";
                GUIStyle rightAlignTitle = new GUIStyle(titleStyle) { fontSize = 28, alignment = TextAnchor.UpperRight };

                GUI.Label(new Rect(x, 60, width, 40), setText, rightAlignTitle);

                GUIStyle rightAlignInfo = new GUIStyle(infoStyle) { alignment = TextAnchor.UpperRight };
                GUI.Label(new Rect(x, 105, width, 70),
                    "<b>Z</b> - Previous Skin\n" +
                    "<b>X</b> - Next Skin",
                    rightAlignInfo);
            }
        }

        // --- CONTROL LISTS (Left side, hidden in Minimal Mode) ---
        if (!minimalUI)
        {
            y += 80;

            // Animation Section
            GUI.Label(new Rect(leftMargin, y, 500, 200),
                "<color=yellow>ANIMATION CONTROLS</color>\n" +
                "<b>A</b> - Previous Animation\n" +
                "<b>D</b> - Next Animation\n" +
                "<b>SPACE</b> - Restart\n" +
                "<b>P</b> - Pause / Resume",
                infoStyle);

            y += 180;

            // Camera Section
            GUI.Label(new Rect(leftMargin, y, 500, 420),
                "<color=yellow>CAMERA CONTROLS</color>\n" +
                "<b>Q / E</b> - Rotate Orbit\n" +
                "<b>W / S</b> - Zoom In/Out\n" +
                "<b>↑ / ↓</b> - Height\n" +
                "<b>← / →</b> - Side Pan\n" +
                "<b>R</b> - Reset View\n" +
                "<b>C</b> - Toggle Auto-Rotate\n" +
                "<b>V</b> - Start Presentation\n" +
                "<b>H</b> - Hide All UI\n" +
                "<b>L</b> - Minimal Mode",
                infoStyle);
        }

        GUI.matrix = oldMatrix;
    }

    /// <summary>
    /// Updates animator, texture controller and camera
    /// when a new enemy is activated.
    /// </summary>
    /// <param name="enemy">Enemy GameObject to activate.</param>
    public void SetActiveEnemy(GameObject enemy)
    {
        animator = enemy.GetComponentInChildren<Animator>();
        textureSetController = enemy.GetComponentInChildren<TextureSetController>();

        UniversalAnimationViewer newViewer = enemy.GetComponentInChildren<UniversalAnimationViewer>();

        if (cameraController != null)
        {
            cameraController.SetTarget(enemy.transform);

            if (newViewer != null)
            {
                cameraController.AdaptToModel(newViewer.alturaManual, newViewer.zoomPersonalizado);
            }
        }

        showUI = true;
        PlayAnimation(0);
    }
}

}

