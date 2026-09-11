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
// 씬을 전환하기 위한 메소드
// GameManager.Instance.Scene.ChangeScene(SceneType)을 통해 실행해주세요.
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
        currentScene = scenes[SceneType.Main]; 
    }

    private void Start()
    {
        if (loading == null)
            loading = GameObject.Instantiate(loadingPrefab);
    }
    public void ChangeScene(SceneType nextSceneType, bool isForced = false)
    { 
        if (isChangingScene)
            return;

        if ((currentScene != null && currentScene.SceneType == nextSceneType) && !isForced)
            return;

        StartCoroutine(ChangeSceneRoutine(nextSceneType));
    }
    public void RestartScene(SceneType nextSceneType)
    { 
        if (isChangingScene)
            return;
         
        if(currentScene.SceneType == SceneType.GameLoop && nextSceneType == SceneType.GameLoop)
            StartCoroutine(ChangeSceneRoutine(nextSceneType));
    }

    private IEnumerator ChangeSceneRoutine(SceneType nextSceneType)
    {
        isChangingScene = true;

        yield return loading.OpenLoading();

        yield return currentScene?.Exit();

        SceneBase nextScene = scenes[nextSceneType];

        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene.SceneName);

        currentScene = nextScene;

        while (!operation.isDone)
            yield return null;

        yield return currentScene.PrepareBeforeReveal();

        yield return loading.CloseLoading();

        yield return currentScene.Enter();

        isChangingScene = false;
    }
}