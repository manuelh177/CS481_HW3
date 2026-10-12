using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EnvironmentNavigator : MonoBehaviour
{
    private static EnvironmentNavigator instance;

    [System.Serializable]
    public struct EnvironmentConfig
    {
        public string sceneName;
        public int obstacleCount; // Only used for circular/rectangular scenes
        public string label;
    }

    private readonly EnvironmentConfig[] environments = new EnvironmentConfig[]
    {
        new EnvironmentConfig { sceneName = "RandomCircularEnvironment", obstacleCount = 20, label = "20 Circles" },
        new EnvironmentConfig { sceneName = "RandomCircularEnvironment", obstacleCount = 30, label = "30 Circles" },
        new EnvironmentConfig { sceneName = "RandomCircularEnvironment", obstacleCount = 100, label = "100 Circles" },
        new EnvironmentConfig { sceneName = "RandomRectangularEnvironment", obstacleCount = 20, label = "20 Rectangles" },
        new EnvironmentConfig { sceneName = "RandomRectangularEnvironment", obstacleCount = 30, label = "30 Rectangles" },
        new EnvironmentConfig { sceneName = "RandomRectangularEnvironment", obstacleCount = 100, label = "100 Rectangles" },
        new EnvironmentConfig { sceneName = "AstarEnvironment", obstacleCount = 0, label = "A* Environment" },
        new EnvironmentConfig { sceneName = "OfficeEnvironment", obstacleCount = 0, label = "Office Environment" }
    };

    private static int currentIndex = 0;

    private void Awake()
    {
        // Singleton pattern to persist across all scene transitions
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Right arrow: Next environment
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            currentIndex = (currentIndex + 1) % environments.Length;
            LoadCurrentEnvironment();
        }
        // Left arrow: Previous environment
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = environments.Length - 1;
            LoadCurrentEnvironment();
        }
    }

    private void LoadCurrentEnvironment()
    {
        EnvironmentConfig config = environments[currentIndex];
        Scene currentScene = SceneManager.GetActiveScene();

        // If we are staying in the same scene (e.g., 20 -> 30 circles), just reconfigure without reloading
        if (currentScene.name == config.sceneName)
        {
            ApplyEnvironmentSettings(config);
        }
        else
        {
            SceneManager.LoadScene(config.sceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyEnvironmentSettings(environments[currentIndex]);
    }

    private void ApplyEnvironmentSettings(EnvironmentConfig config)
    {
        // Update circle generator if present in the scene
        GenerateCircles circleGen = Object.FindFirstObjectByType<GenerateCircles>();
        if (circleGen != null)
        {
            circleGen.SetObstacleCount(config.obstacleCount);
            circleGen.GenerateObstacles();
        }

        // Update rectangle generator if present in the scene
        GenerateRectangles rectGen = Object.FindFirstObjectByType<GenerateRectangles>();
        if (rectGen != null)
        {
            rectGen.SetObstacleCount(config.obstacleCount);
            rectGen.GenerateObstacles();
        }

        // Reset any existing path or selection markers
        PathfindingController controller = Object.FindFirstObjectByType<PathfindingController>();
        if (controller != null)
        {
            controller.ResetPath();
        }
    }

    // Optional on-screen label showing the active environment
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        style.normal.textColor = Color.black;

        string info = $"[{currentIndex + 1}/8] {environments[currentIndex].label} (Use Left/Right Arrows)";
    
        float labelWidth = 600f;
        float paddingRight = 20f;
        float posX = Screen.width - labelWidth - paddingRight;

        GUI.Label(new Rect(posX, 20, labelWidth, 40), info, style);
    }
}
