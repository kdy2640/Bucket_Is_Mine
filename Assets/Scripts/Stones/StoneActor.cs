using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HPHandler), typeof(StonePresenter), typeof(Rigidbody))]
public sealed class StoneActor : MonoBehaviour
{
    [SerializeField] private HPHandler hpHandler;
    [SerializeField] private StonePresenter presenter;

    private StoneDataSO dataSO;
    private GameObject modelInstance;
    private Collider[] modelColliders;
    private bool isBreaking;

    public StoneDataSO DataSO => dataSO;
    public HPHandler HP => hpHandler;
    public StonePresenter Presenter => presenter;

    private void Awake()
    {
        hpHandler.SubscribeDying(OnDied);
    }

    public void SetData(int stoneID)
    {
        StoneDataSO data = StoneDataDB.GetData(stoneID);
        presenter.StopCurrentTween();

        if (modelInstance != null)
        {
            modelInstance.SetActive(false);
            Destroy(modelInstance);
        }

        dataSO = data;
        modelInstance = Instantiate(data.ModelPrefab, transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelColliders = modelInstance.GetComponentsInChildren<Collider>(true);
        foreach (Collider modelCollider in modelColliders)
        {
            modelCollider.gameObject.layer = gameObject.layer;
            modelCollider.isTrigger = true;
        }

        isBreaking = false;
        hpHandler.SetMaxHealth(data.MaxHealth);
        presenter.Initialize(modelInstance.transform);
        presenter.PlaySpawnTween();
    }

    public void TakeDamage(float damage)
    {
        if (isBreaking)
            return;

        hpHandler.TakeDamage(damage);
        if (!hpHandler.IsDead)
            presenter.PlayHitReactionTween();
    }

    private void OnDied()
    {
        if (isBreaking)
            return;

        isBreaking = true;
        foreach (Collider modelCollider in modelColliders)
            modelCollider.enabled = false;

        GameManager.Instance.StockManager.AddOre(new List<OreAmount>(dataSO.RewardList));
        presenter.PlayBreakTween(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        hpHandler.UnSubscribeDying(OnDied);
    }
}
