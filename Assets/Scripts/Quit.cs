using Symvolution.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class Quit : MonoBehaviour
{
    [SerializeField] private Button ShowQuit;
    [SerializeField] private GameObject QuitConfirmPanel;
    [SerializeField] private Button OnQuit;
    [SerializeField] private Button Close;
    
    [Header("Localize")]
    [SerializeField] private Text localize1;
    void Start()
    {
        ShowQuit.onClick.AddListener(()=>QuitConfirmPanel.SetActive(true));
        Close.onClick.AddListener(()=>QuitConfirmPanel.SetActive(false));
        OnQuit.onClick.AddListener(Application.Quit);
        SetLocalize();
    }
    
    private void SetLocalize()
    {
        localize1.font = Localize.GetLocalizeFont();
        localize1.text = Localize.Get("QUIT");
    }

}
