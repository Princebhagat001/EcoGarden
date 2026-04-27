using UnityEngine;
using System.Collections.Generic;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.Core;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.CameraControllers;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Controllers
{
    /// <summary>
    /// Manages group mode, where multiple models are shown together.
    /// Spawns lineup clones, synchronizes skins, and positions the camera.
    /// </summary>
    public class GroupDisplayManager : MonoBehaviour
{
    [Header("CONFIGURATION")]

    /// <summary>
    /// Prefabs used to spawn the group lineup.
    /// </summary>
    [SerializeField] private GameObject[] modelPrefabs;

    /// <summary>
    /// Spawn points for each model in the lineup.
    /// </summary>
    [SerializeField] private Transform[] spawnPoints;

    /// <summary>
    /// Key used to toggle group preview mode.
    /// </summary>
    [SerializeField] private KeyCode groupToggleKey = KeyCode.G;

    /// <summary>
    /// Reference to the enemy selector to restore single preview mode.
    /// </summary>
    [SerializeField] private EnemySelector enemySelector;

    [Header("GROUP CAMERA")]

    /// <summary>
    /// Fixed camera position used during group preview.
    /// </summary>
    [SerializeField] private Vector3 groupCameraPosition = new Vector3(0, 2, -8);

    /// <summary>
    /// Fixed camera rotation used during group preview.
    /// </summary>
    [SerializeField] private Vector3 groupCameraRotation = new Vector3(10, 0, 0);

    /// <summary>
    /// Active instantiated group clones.
    /// </summary>
    private readonly List<GameObject> activeClones = new List<GameObject>();

    /// <summary>
    /// Texture controllers collected from active group clones.
    /// </summary>
    private readonly List<TextureSetController> textureControllers = new List<TextureSetController>();

    /// <summary>
    /// Indicates whether group preview mode is active.
    /// </summary>
    private bool isGroupModeActive = false;

    /// <summary>
    /// Global skin index used for synchronized skin switching.
    /// </summary>
    private int globalSkinIndex = 0;

    void Update()
    {
        if (Input.GetKeyDown(groupToggleKey))
            ToggleGroupMode();

        if (!isGroupModeActive) return;

        if (Input.GetKeyDown(KeyCode.T)) CycleAllSkinsSequentially();
        if (Input.GetKeyDown(KeyCode.Y)) RandomizeAllSkins();

        // Reset skins and camera
        if (Input.GetKeyDown(KeyCode.J))
        {
            globalSkinIndex = 0;
            foreach (var ctrl in textureControllers)
                ctrl.SetTextureSet(0);

            PositionCameraForGroup();
            Debug.Log("Group view reset.");
        }

        // Re-center group camera
        if (Input.GetKeyDown(KeyCode.R))
        {
            PositionCameraForGroup();
            Debug.Log("Group camera re-centered.");
        }
    }

    /// <summary>
    /// Toggles between single preview mode and group preview mode.
    /// </summary>
    public void ToggleGroupMode()
    {
        isGroupModeActive = !isGroupModeActive;

        if (isGroupModeActive)
        {
            enemySelector?.HideAllEnemies();
            SpawnGroup();
            PositionCameraForGroup();
        }
        else
        {
            CleanupGroup();
            enemySelector?.RestoreLastEnemy();
        }
    }

    /// <summary>
    /// Spawns the group lineup and disables individual viewers on cloned models.
    /// </summary>
    private void SpawnGroup()
    {
        CleanupGroup();
        textureControllers.Clear();

        for (int i = 0; i < modelPrefabs.Length && i < spawnPoints.Length; i++)
        {
            GameObject clone = Instantiate(
                modelPrefabs[i],
                spawnPoints[i].position,
                spawnPoints[i].rotation
            );

            // Disable individual viewers to avoid UI/camera conflicts
            var viewer = clone.GetComponentInChildren<UniversalAnimationViewer>();
            if (viewer != null)
                viewer.enabled = false;

            // Collect texture controllers for global skin management
            var textureController = clone.GetComponentInChildren<TextureSetController>();
            if (textureController != null)
                textureControllers.Add(textureController);

            activeClones.Add(clone);
        }
    }

    /// <summary>
    /// Advances all models to the next texture set in sync.
    /// </summary>
    private void CycleAllSkinsSequentially()
    {
        globalSkinIndex++;

        foreach (var ctrl in textureControllers)
        {
            int targetSet = globalSkinIndex % ctrl.TotalSets;
            ctrl.SetTextureSet(targetSet);
        }
    }

    /// <summary>
    /// Randomizes skins across all models while avoiding immediate repetition.
    /// </summary>
    private void RandomizeAllSkins()
    {
        if (textureControllers.Count == 0) return;

        int totalAvailableSets = textureControllers[0].TotalSets;
        List<int> skinPool = new List<int>();

        for (int i = 0; i < totalAvailableSets; i++)
            skinPool.Add(i);

        // Shuffle pool (Fisher–Yates)
        for (int i = 0; i < skinPool.Count; i++)
        {
            int randomIndex = Random.Range(i, skinPool.Count);
            (skinPool[i], skinPool[randomIndex]) = (skinPool[randomIndex], skinPool[i]);
        }

        // Avoid assigning the same skin as before when possible
        for (int i = 0; i < textureControllers.Count; i++)
        {
            int assignedSkin = skinPool[i % skinPool.Count];
            int currentSkin = textureControllers[i].CurrentSet;

            if (assignedSkin == currentSkin && skinPool.Count > 1)
            {
                int nextIndex = (i + 1) % skinPool.Count;
                (skinPool[i % skinPool.Count], skinPool[nextIndex]) =
                    (skinPool[nextIndex], skinPool[i % skinPool.Count]);
            }
        }

        // Apply skins
        for (int i = 0; i < textureControllers.Count; i++)
        {
            int finalSkin = skinPool[i % skinPool.Count];
            textureControllers[i].SetTextureSet(finalSkin);
        }

        Debug.Log("<color=green>Random skins applied (unique and non-repeating).</color>");
    }

    /// <summary>
    /// Forces the camera into a fixed group preview position.
    /// </summary>
    private void PositionCameraForGroup()
    {
        var cameraController = FindFirstObjectByType<ViewerCameraController>();

        if (cameraController != null)
        {
            cameraController.ForceGroupView(groupCameraPosition, groupCameraRotation);
        }
        else if (UnityEngine.Camera.main != null)
        {
            UnityEngine.Camera.main.transform.position = groupCameraPosition;
            UnityEngine.Camera.main.transform.eulerAngles = groupCameraRotation;
        }
    }

    /// <summary>
    /// Destroys all active group clones and restores camera behavior.
    /// </summary>
    private void CleanupGroup()
    {
        foreach (GameObject clone in activeClones)
            if (clone != null)
                Destroy(clone);

        activeClones.Clear();

        var cameraController = FindFirstObjectByType<ViewerCameraController>();
        cameraController?.ResetCamera();
    }

    /// <summary>
    /// Emergency cleanup used when switching modes externally.
    /// </summary>
    public void ForceCleanup()
    {
        if (!isGroupModeActive) return;

        isGroupModeActive = false;
        CleanupGroup();

        var cameraController = UnityEngine.Camera.main?.GetComponent<ViewerCameraController>();
        if (cameraController != null)
            cameraController.enabled = true;
    }

    /// <summary>
    /// Draws the group mode UI including synchronization status, 
    /// lineup information, and global skin controls.
    /// </summary>
    void OnGUI()
    {
        if (ViewerCameraController.UIHiddenGlobal) return;
        if (!isGroupModeActive) return;

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
            normal = { textColor = Color.cyan }
        };
        GUIStyle shadowStyle = new GUIStyle(titleStyle)
        {
            normal = { textColor = Color.black }
        };
        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            normal = { textColor = Color.white },
            richText = true
        };

        // --- LEFT TITLE WITH SHADOW ---
        GUI.Label(new Rect(leftMargin + 2, y + 2, 900, 55), "▶ GROUP MODE (LINEUP VIEW)", shadowStyle);
        GUI.Label(new Rect(leftMargin, y, 900, 55), "▶ GROUP MODE (LINEUP VIEW)", titleStyle);

        y += 80;

        // --- CONTROL LIST SECTION ---
        GUI.Label(new Rect(leftMargin, y, 600, 500),
            "<color=cyan>GROUP CONTROLS</color>\n" +
            "<b>G</b> - Exit Group Mode\n" +
            "<b>T</b> - Sync All Skins (Sequential)\n" +
            "<b>Y</b> - Randomize All Skins\n" +
            "<b>J</b> - Reset Skins & Camera\n" +
            "<b>R</b> - Recenter Group Camera\n" +
            "<b>H</b> - Hide All UI",
            infoStyle);

        // --- RIGHT STATUS SECTION WITH SHADOW ---
        if (textureControllers.Count > 0)
        {
            float virtualWidth = Screen.width / scale;
            float width = 500f;
            float x = virtualWidth - width - rightMargin;

            GUIStyle rightAlign = new GUIStyle(titleStyle) { alignment = TextAnchor.UpperRight };
            GUIStyle rightShadow = new GUIStyle(shadowStyle) { alignment = TextAnchor.UpperRight };

            // Draw Shadow then Title for depth
            GUI.Label(new Rect(x + 2, 12, width, 55), "MODEL LINEUP", rightShadow);
            GUI.Label(new Rect(x, 10, width, 55), "MODEL LINEUP", rightAlign);

            // Global Skin Synchronization Status
            string skinStatus = $"Global Sync: Skin {(globalSkinIndex % textureControllers[0].TotalSets) + 1}";
            GUIStyle rightInfoTitle = new GUIStyle(titleStyle) { fontSize = 28, alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(x, 60, width, 40), skinStatus, rightInfoTitle);

            // Active Models Counter
            GUIStyle rightInfo = new GUIStyle(infoStyle) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(x, 105, width, 35), $"{activeClones.Count} Models Displayed", rightInfo);
        }

        GUI.matrix = oldMatrix;
    }

}

}
