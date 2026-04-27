using UnityEngine;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.Core;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Controllers
{
    /// <summary>
    /// Switches between enemy roots and keeps only one active at a time.
    /// Also updates the active viewer reference.
    /// </summary>
    public class EnemySelector : MonoBehaviour
{
    /// <summary>
    /// Reference to the animation viewer responsible for controlling animations and camera behavior.
    /// </summary>
    public UniversalAnimationViewer viewer;

    [Header("Drag enemy root objects here")]

    /// <summary>
    /// Array containing all enemy root GameObjects available for selection.
    /// Only one enemy will be active at a time.
    /// </summary>
    public GameObject[] enemies;

    [Header("Group Display Manager Reference")]

    /// <summary>
    /// Optional reference to the group display manager.
    /// Used to clean up cloned enemies when switching back to individual view.
    /// </summary>
    public GroupDisplayManager groupManager;

    /// <summary>
    /// Index of the currently active enemy.
    /// </summary>
    private int current = 0;

    /// <summary>
    /// Initializes the selector by activating the first enemy in the list.
    /// </summary>
    void Start()
    {
        ActivateEnemy(0);
    }

    /// <summary>
    /// Listens for numeric key input (1–5) to switch between enemies.
    /// Automatically disables group mode before activating a single enemy.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { CheckCleanup(); ActivateEnemy(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { CheckCleanup(); ActivateEnemy(1); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { CheckCleanup(); ActivateEnemy(2); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { CheckCleanup(); ActivateEnemy(3); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { CheckCleanup(); ActivateEnemy(4); }
    }

    /// <summary>
    /// Ensures that any previously spawned group enemies are removed
    /// before activating a single enemy.
    /// </summary>
    void CheckCleanup()
    {
        if (groupManager != null)
        {
            groupManager.ForceCleanup();
        }
    }

    /// <summary>
    /// Activates a specific enemy by index and deactivates all others.
    /// Also updates the animation viewer reference.
    /// </summary>
    /// <param name="index">Index of the enemy to activate.</param>
    void ActivateEnemy(int index)
    {
        if (enemies == null || enemies.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, enemies.Length - 1);

        for (int i = 0; i < enemies.Length; i++)
            enemies[i].SetActive(i == index);

        current = index;

        viewer = enemies[current].GetComponentInChildren<UniversalAnimationViewer>();

        if (viewer != null)
            viewer.SetActiveEnemy(enemies[current]);

        Debug.Log("👾 Active enemy: " + enemies[current].name);
    }

    /// <summary>
    /// Deactivates all enemy GameObjects.
    /// Useful when switching to group display mode.
    /// </summary>
    public void HideAllEnemies()
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.SetActive(false);
        }
    }

    /// <summary>
    /// Reactivates the last selected enemy.
    /// Intended to restore individual view after exiting group mode.
    /// </summary>
    public void RestoreLastEnemy()
    {
        ActivateEnemy(current);
    }
    }
}
