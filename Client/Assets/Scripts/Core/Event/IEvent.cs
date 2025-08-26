using System;

// 事件基础接口
public interface IEvent
{
}

// 事件订阅接口
public interface IEventSubscription
{
    void Unsubscribe();
    bool IsMatch(Type eventType, Delegate handler);
}

// 事件订阅实现
public class EventSubscription<T> : IEventSubscription where T : IEvent
{
    private readonly Action<T> _handler;
    
    public EventSubscription(Action<T> handler)
    {
        _handler = handler;
    }
    
    public void Unsubscribe()
    {
        EventManager.Instance.Unsubscribe(_handler);
    }
    
    public bool IsMatch(Type eventType, Delegate handler)
    {
        return eventType == typeof(T) && ReferenceEquals(_handler, handler);
    }
} 