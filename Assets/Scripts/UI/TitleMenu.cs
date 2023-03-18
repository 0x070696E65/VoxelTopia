using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class TitleMenu : MonoBehaviour
    {
        public GameObject mainMenuObject;
        public GameObject settingsObject;

        [Header("Main Menu UI Elements")]
        public TextMeshProUGUI seedField;

        [Header("Setting Menu UI Elements")]
        public Slider viewDinstanceSlider;
        public TextMeshProUGUI viewDstText;
        public Slider mouseSlider;
        public TextMeshProUGUI mouseTxtSlider;
        public Toggle threadingToggle;
        public Toggle chunkAnimToggle;
        public TMP_Dropdown clouds;
    
        [SerializeField] private Button startGame;
        [SerializeField] private Button moveSettings;
        [SerializeField] private Button quitGame;
        [SerializeField] private Button done;

        private Settings settings;
        private GameInputs gameInputs;
        //private Vector2 cursorPosition;
        [SerializeField] private float mouseSpeed;
    
        private void Awake()
        {
            if (!File.Exists(Application.dataPath + "/settings.cfg"))
            {
                Debug.Log("No settings file found, creating new one.");
                settings = new Settings();
                var jsonExport = JsonUtility.ToJson(settings);
                File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
            }
            else
            {
                Debug.Log("settings file found, loading settings.");
                var jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
                settings = JsonUtility.FromJson<Settings>(jsonImport);
            }
        }

        private void Start()
        {
            //gameInputs = new GameInputs();
            //gameInputs.UI.CursorClick.performed += OnCursorClick;
            //gameInputs.Enable();
            
            // cursorPosition = new Vector2(Screen.width / 2, Screen.height / 2);
        
            startGame.onClick.AddListener(StartGame);
            quitGame.onClick.AddListener(QuitGame);
            moveSettings.onClick.AddListener(EnterSettings);
            done.onClick.AddListener(LeaveSettings);

            viewDinstanceSlider.onValueChanged.AddListener(UpdateViewDstSlider);
            mouseSlider.onValueChanged.AddListener(UpdateMouseSlider);
            
            if (Gamepad.current == null) Destroy(GetComponent<VirtualMouseInput>());
        }

        private void Update()
        {
            /*
            if (Gamepad.current == null) return;

            var mouse = InputSystem.AddDevice<Mouse>();

            var delta = gameInputs.UI.GamepadMouse.ReadValue<Vector2>() * mouseSpeed;
            // Somewhere in update.
            //var mouseDelta = gameInputs.UI.CursorClick.ReadValue<Vector2>() * mouseSpeed;
            var currentPosition = mouse.position.ReadValue();
            InputSystem.QueueStateEvent(mouse,
                new MouseState
                {
                    position = currentPosition + delta,
                    delta = delta,
                    // Set other stuff like button states...
                });
            */
            /*cursorPosition += delta * mouseSpeed;
            cursorPosition.x = Mathf.Clamp(cursorPosition.x, 0, Screen.width);
            cursorPosition.y = Mathf.Clamp(cursorPosition.y, 0, Screen.height);
            Mouse.current.WarpCursorPosition(cursorPosition);*/
        }

        private void StartGame()
        {
            VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.WorldSizeInChunks;
            SceneManager.LoadScene("World", LoadSceneMode.Single);
        }

        private void EnterSettings()
        {
            viewDinstanceSlider.value = settings.viewDistance;
            UpdateViewDstSlider(viewDinstanceSlider.value);
            mouseSlider.value = settings.mouseSensitivity;
            UpdateMouseSlider(mouseSlider.value);
            threadingToggle.isOn = settings.enableThreading;
            chunkAnimToggle.isOn = settings.enableAnimatedChunks;
            clouds.value = (int) settings.clouds;
        
            mainMenuObject.SetActive(false);
            settingsObject.SetActive(true);
        }

        private void LeaveSettings()
        {
            settings.viewDistance = (int) viewDinstanceSlider.value;
            settings.mouseSensitivity = mouseSlider.value;
            settings.enableThreading = threadingToggle.isOn;
            settings.enableAnimatedChunks = chunkAnimToggle.isOn;
            settings.clouds = (CloudStyle)clouds.value;

            var jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        
            mainMenuObject.SetActive(true);
            settingsObject.SetActive(false);
        }
    
        private static void QuitGame()
        {
            Application.Quit();
        }

        private void UpdateViewDstSlider(float value)
        {
            viewDstText.text = "View Distance: " + value;
        }

        private void UpdateMouseSlider(float value)
        {
            mouseTxtSlider.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");
        }

        /*private void OnCursorClick(InputAction.CallbackContext context)
        {
            moveSettings.onClick?.Invoke();
        }*/
    }
}
