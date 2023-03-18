using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject loadUI;
    [SerializeField] private Slider slider;
    [SerializeField] private string nextSceneName;
    
    private AsyncOperation async;
    
    void Start()
    {
        button.onClick.AddListener(NextScene);
    }

    private void NextScene() {
        //　ロード画面UIをアクティブにする
        loadUI.SetActive(true);

        //　コルーチンを開始
        StartCoroutine("LoadData");
    }

    IEnumerator LoadData() {
        // シーンの読み込みをする
        async = SceneManager.LoadSceneAsync(nextSceneName);

        //　読み込みが終わるまで進捗状況をスライダーの値に反映させる
        while(!async.isDone) {
            var progressVal = Mathf.Clamp01(async.progress / 0.9f);
            slider.value = progressVal;
            yield return null;
        }
    }
}
