using System.Collections;
using UnityEngine;

public class MainScene : SceneBase
{
    public override SceneType SceneType => SceneType.Main;
    public override string SceneName => "MainScene";

    public override IEnumerator Enter()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield break;
    }

}
