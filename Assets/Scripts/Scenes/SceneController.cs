using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    Main,
    Upgrade,
    GameLoop
}

// GameManager.Instance.SceneController.ChangeScene(SceneType)로 전환합니다.
public class SceneController : MonoBehaviour
{
    [SerializeField] private UI_Loading loadingPrefab;
    private Dictionary<SceneType, SceneBase> scenes;
    private SceneBase currentScene;
    public SceneType currenSceneType => currentScene.SceneType;

    private bool isChangingScene;
    private UI_Loading loading;

    private void Awake()
    {
        scenes = new Dictionary<SceneType, SceneBase>
        {
            { SceneType.Main, new MainScene() },
            { SceneType.Upgrade, new UpgradeScene() },
            { SceneType.GameLoop, new GameLoopScene() }
        };
        string sceneName = SceneManager.GetActiveScene().name;
        switch (sceneName)
        {
            case "MainScene": currentScene = scenes[SceneType.Main]; break;
            case "UpgradeScene": currentScene = scenes[SceneType.Upgrade]; break;
            case "GameLoopScene": currentScene = scenes[SceneType.GameLoop]; break;
        }
        isChangingScene = true;
    }

    private IEnumerator Start()
    {
        loading = Instantiate(loadingPrefab);
        yield return loading.OpenLoading();
        yield return currentScene.PrepareBeforeReveal();
        yield return loading.CloseLoading();
        yield return currentScene.Enter();
        isChangingScene = false;
    }

    public void ChangeScene(SceneType nextSceneType, bool isForced = false)
    {
        if (isChangingScene)
            return;
        if (currentScene.SceneType == nextSceneType && !isForced)
            return;
        StartCoroutine(ChangeSceneRoutine(nextSceneType));
    }

    public void RestartScene(SceneType nextSceneType)
    {
        if (isChangingScene)
            return;
        if (currentScene.SceneType == SceneType.GameLoop && nextSceneType == SceneType.GameLoop)
            StartCoroutine(ChangeSceneRoutine(nextSceneType));
    }

    private IEnumerator ChangeSceneRoutine(SceneType nextSceneType)
    {
        isChangingScene = true;
        yield return currentScene.Exit();
        yield return loading.OpenLoading();
        SceneBase nextScene = scenes[nextSceneType];
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene.SceneName);
        while (!operation.isDone)
            yield return null;
        currentScene = nextScene;
        yield return currentScene.PrepareBeforeReveal();
        yield return loading.CloseLoading();
        yield return currentScene.Enter();
        isChangingScene = false;
    }
}
