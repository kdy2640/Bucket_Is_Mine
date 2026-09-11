using System.Collections;
using UnityEngine;

public class GameLoopScene : SceneBase
{
    public override SceneType SceneType => SceneType.GameLoop;
    public override string SceneName => "GameLoopScene";

    public override IEnumerator PrepareBeforeReveal()
    { 
        yield return null;
    }

    public override IEnumerator Enter()
    {
        yield return null;
    }

    public override IEnumerator Exit()
    { 
        yield return null;
    }
}