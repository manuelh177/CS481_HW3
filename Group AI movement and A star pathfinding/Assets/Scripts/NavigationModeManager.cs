
using UnityEngine;
using UnityEngine.InputSystem;

public class NavigationModeManager : MonoBehaviour
{
    public enum NavigationMode
    {
        AStar,
        PotentialFields,
        Combined
    }

    [Header("Mode Settings")]
    [SerializeField] private NavigationMode currentMode = NavigationMode.AStar;

    [Header("References")]
    [SerializeField] private PathfindingController pathfindingController;

    public bool IsPotentialFieldsMode
    {
        get
        {
            return currentMode == NavigationMode.PotentialFields ||
                   currentMode == NavigationMode.Combined;
        }
    }

    public bool IsCombinedMode
    {
        get { return currentMode == NavigationMode.Combined; }
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
            currentMode = (NavigationMode)(
                ((int)currentMode + 1) % 3
            );

            UpdateMode();
        }
    }

    private void UpdateMode()
    {
        if (pathfindingController != null)
        {
            pathfindingController.enabled =
                currentMode == NavigationMode.AStar;
        }

        Debug.Log("Navigation Mode: " + currentMode);
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);

        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.black;

        string modeText = currentMode switch
        {
            NavigationMode.AStar => "Mode: A* Pathfinding (P)",
            NavigationMode.PotentialFields => "Mode: Potential Fields (P)",
            NavigationMode.Combined => "Mode: A* + Potential Fields (P)",
            _ => "Unknown Mode"
        };

        GUI.Label(
            new Rect(15, 10, 450, 40),
            modeText,
            style
        );
    }
}
