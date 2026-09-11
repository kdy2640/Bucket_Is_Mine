using System.Collections;

public class UpgradeScene : SceneBase
{
    public override SceneType SceneType => SceneType.Upgrade;
    public override string SceneName => "UpgradeScene";

    public override IEnumerator Enter()
    {
        // 업그레이드 씬 진입 시 데이터 준비
        yield return null;
    }

    public override IEnumerator Exit()
    {
        // 업그레이드 결과 확정
        // RuntimeStat 계산 준비
        yield return null;
    }
}