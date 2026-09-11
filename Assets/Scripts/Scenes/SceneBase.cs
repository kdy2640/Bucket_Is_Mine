using System.Collections;

public abstract class SceneBase
{
    public abstract SceneType SceneType { get; }
    public abstract string SceneName { get; }

    public virtual IEnumerator PrepareBeforeReveal() { yield return null; }
    public virtual IEnumerator Enter() { yield return null;  }
    public virtual IEnumerator Exit() { yield return null;  }
}