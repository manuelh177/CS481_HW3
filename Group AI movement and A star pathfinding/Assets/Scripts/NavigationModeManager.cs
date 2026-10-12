
using UnityEngine;
using UnityEngine.InputSystem;

public class NavigationModeManager : MonoBehaviour
{
    [Header("Mode Settings")]
    [SerializeField] private bool potentialFieldsMode = false;

    [Header("References")]
    [SerializeField] private PathfindingController pathfindingController;

    public bool IsPotentialFieldsMode
    {
        get { return potentialFieldsMode; }
    }

    private void Start()
    {
        UpdateMode();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            potentialFieldsMode = !potentialFieldsMode;

            UpdateMode();
        }
    }

    private void UpdateMode()
    {
        // Disable A* mouse controls during Potential Fields mode.
        if (pathfindingController != null)
        {
            pathfindingController.enabled = !potentialFieldsMode;
        }

        if (potentialFieldsMode)
        {
            Debug.Log("Mode: Potential Fields");
        }
        else
        {
            Debug.Log("Mode: A* Pathfinding");
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);

        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.black;

        string modeText = potentialFieldsMode
            ? "Mode: Potential Fields (P)"
            : "Mode: A* Pathfinding (P)";

        GUI.Label(
            new Rect(15, 10, 350, 40),
            modeText,
            style
        );
    }
}
