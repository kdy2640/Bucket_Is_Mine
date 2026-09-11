using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLoopEventType
{
    BeforeLoopStarted,
    LoopStarted,
    LoopEnded,
    StoneSpawned,
    StoneDestroyed 
}

public interface IGameLoopEventSubscribable
{
    void Subscribe(GameLoopEventType eventType, Action action);
    void Unsubscribe(GameLoopEventType eventType, Action action);
}

// 게임루프 중 이벤트를 등록하는 클래스.
// GameManager.Instance.GameLoop.Events.Subscribe(type, action)을 통해 호출해주세요.
public class GameLoopEventManager : IGameLoopEventSubscribable
{
    private readonly Dictionary<GameLoopEventType, Action> events = new();

    public void Subscribe(GameLoopEventType eventType, Action action)
    {
        if (!events.ContainsKey(eventType))
            events[eventType] = null;

        events[eventType] += action;
    }

    public void Unsubscribe(GameLoopEventType eventType, Action action)
    {
        if (!events.ContainsKey(eventType))
            return;

        events[eventType] -= action;
    }

    public void Invoke(GameLoopEventType eventType)
    {
        if (!events.TryGetValue(eventType, out Action action))
            return;

        action?.Invoke();
    }
}