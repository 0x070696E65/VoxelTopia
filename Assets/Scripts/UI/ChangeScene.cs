using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum CurrentScene
{
    Editor,
    World,
    Shop
}
public class ChangeScene : MonoBehaviour
{
    [SerializeField] private Button toWorld;
    [SerializeField] private Button toShop;
    [SerializeField] private Button toEditor;
    [SerializeField] private CurrentScene currentScene;
    
    void Start()
    {
        SetButtonInteractable(currentScene);
        toWorld.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("World");
        });
        toShop.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Shop");
        });
        toEditor.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Editor");
        });
    }

    void SetButtonInteractable(CurrentScene _currentScene)
    {
        switch (_currentScene)
        {
            case CurrentScene.Editor:
                toEditor.interactable = false;
                toWorld.interactable = true;
                toShop.interactable = true;
                break;
            case CurrentScene.World:
                toEditor.interactable = true;
                toWorld.interactable = false;
                toShop.interactable = true;
                break;
            case CurrentScene.Shop:
                toEditor.interactable = true;
                toWorld.interactable = true;
                toShop.interactable = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_currentScene), _currentScene, null);
        }
    }
}
