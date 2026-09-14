using System;
using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    public bool IsGameLoopScene => GameManager.Instance.SceneController.currenSceneType == SceneType.GameLoop; 
    private float loopDuration = 20f;
     
    private GameLoopEventManager eventManager;  
    private Action<float> OnTick;
    private float timer;
    private bool isRunning = false;

    public float Timer { get { return timer; } }
    public bool IsRunning => isRunning;
    public GameLoopEventManager Events => eventManager;  


    private void Awake()
    { 
        eventManager = new GameLoopEventManager();  
    } 
    public void PrepareReveal()
    {
        timer = loopDuration;
        OnTick?.Invoke(Timer);
    }
    public void StartLoop()
    {
        if (!IsGameLoopScene) return; 
        isRunning = true;

        eventManager.Invoke(GameLoopEventType.LoopStarted);
    }

    private void Update()
    {
        if (!isRunning)
            return;

        timer -= Time.deltaTime; 
        OnTick?.Invoke(Timer);

        if (timer <= 0f)
            EndLoop();
    }

    public void EndLoop()
    {
        if (!isRunning) return;
        if (!IsGameLoopScene) return;
        isRunning = false;
        eventManager.Invoke(GameLoopEventType.LoopEnded);
    }

    public void StopLoop()
    {
        isRunning = false;
    }

    public void SubscribeTick(Action<float> ev)
    {
        OnTick += ev;
    }
    public void UnSubscribeTick(Action<float> ev)
    {
        OnTick -= ev;
    }
    public void Restart()
    {
        GameManager.Instance.SceneController.RestartScene(SceneType.GameLoop);
    }

}