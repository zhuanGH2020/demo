using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 所有View的基类，提供统一的基础功能和Manager访问接口
public abstract class BaseView : MonoBehaviour
{
    private readonly List<IEventSubscription> _subscriptions = new List<IEventSubscription>();
    
    #region Manager访问接口
    
    protected T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        return ResourceManager.Instance.Load<T>(path);
    }
    
    protected ConfigReader GetConfig(string configName)
    {
        return ConfigManager.Instance.GetReader(configName);
    }
    
    protected void SubscribeEvent<T>(Action<T> handler) where T : IEvent
    {
        EventManager.Instance.Subscribe(handler);
        _subscriptions.Add(new EventSubscription<T>(handler));
    }
    
    protected void PublishEvent<T>(T eventData) where T : IEvent
    {
        EventManager.Instance.Publish(eventData);
    }
    
    protected void UnsubscribeEvent<T>(Action<T> handler) where T : IEvent
    {
        EventManager.Instance.Unsubscribe(handler);
        _subscriptions.RemoveAll(s => s.IsMatch(typeof(T), handler));
    }
    
    #endregion
    
    #region UI便捷方法
    
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
    
    protected void LoadAndSetSprite(Image image, string spritePath)
    {
        if (image == null)
        {
            Debug.LogError("Image component is null");
            return;
        }
        
        if (string.IsNullOrEmpty(spritePath))
        {
            Debug.LogError("Sprite path is null or empty");
            return;
        }
        
        Sprite sprite = LoadResource<Sprite>(spritePath);
        if (sprite != null)
        {
            image.sprite = sprite;
        }
        else
        {
            Debug.LogError($"Failed to load sprite: {spritePath}");
        }
    }
    
    #endregion
    
    #region 自动事件管理
    
    protected virtual void OnDestroy()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Unsubscribe();
        }
        _subscriptions.Clear();
    }
    
    #endregion
} 