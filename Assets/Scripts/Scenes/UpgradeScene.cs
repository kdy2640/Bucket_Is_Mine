using System.Collections;
using UnityEngine;

public class UpgradeScene : SceneBase
{
    public override SceneType SceneType => SceneType.Upgrade;
    public override string SceneName => "UpgradeScene";

    public override IEnumerator Enter()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield break;
    }

    public override IEnumerator Exit()
    {
        yield break;
    }

}
