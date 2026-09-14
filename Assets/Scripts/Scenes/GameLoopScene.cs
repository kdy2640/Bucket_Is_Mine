using System.Collections;
using UnityEngine;

public class GameLoopScene : SceneBase
{
    public override SceneType SceneType => SceneType.GameLoop;
    public override string SceneName => "GameLoopScene";
    private PlayerController player;

    public override IEnumerator PrepareBeforeReveal()
    {
        yield return null;
        player = Object.FindFirstObjectByType<PlayerController>();
        player.SetGameplayActive(false);
        TerrainManager terrain = Object.FindFirstObjectByType<TerrainManager>();
        while (!terrain.IsInitialLoadComplete)
            yield return null;
        GameManager.Instance.GameLoopManager.PrepareReveal();
    }

    public override IEnumerator Enter()
    {
        GameManager.Instance.GameLoopManager.Events.Subscribe(GameLoopEventType.LoopEnded, OnLoopEnded);
        player.SetGameplayActive(true);
        GameManager.Instance.GameLoopManager.StartLoop();
        yield break;
    }

    public override IEnumerator Exit()
    {
        GameManager.Instance.GameLoopManager.Events.Unsubscribe(GameLoopEventType.LoopEnded, OnLoopEnded);
        GameManager.Instance.GameLoopManager.StopLoop();
        player.SetGameplayActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield break;
    }

    private void OnLoopEnded()
    {
        player.SetGameplayActive(false);
        GameManager.Instance.SceneController.ChangeScene(SceneType.Upgrade);
    }
}
