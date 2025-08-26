using System;
using System.Collections.Generic;
using UnityEngine;

// UI层级枚举，定义UI显示层次
public enum UILayer
{
    Background = 0,  // 背景层
    Normal = 100,    // 普通UI层
    Popup = 200,     // 弹窗层
    System = 400     // 系统层
}

// UI管理器，统一管理所有UI界面的显示隐藏和生命周期
public class UIManager
{
    private static UIManager _instance;
    public static UIManager Instance => _instance ??= new UIManager();
    
    private Dictionary<Type, BaseView> _views = new Dictionary<Type, BaseView>();
    private Canvas _uiCanvas;
    private CanvasGroup _canvasGroup;
    
    private UIManager()
    {
        InitializeUICanvas();
    }
    
    #region UI生命周期管理
    
    public bool Show<T>(UILayer layer = UILayer.Normal) where T : BaseView
    {
        Type viewType = typeof(T);
        
        if (_views.TryGetValue(viewType, out BaseView existingView) && 
            existingView != null && existingView.gameObject.activeInHierarchy)
        {
            return true;
        }
        
        BaseView view = GetOrCreateView<T>();
        if (view == null) return false;
        
        _views[viewType] = view;
        
        SetViewLayer(view, layer);
        
        view.gameObject.SetActive(true);
        
        EventManager.Instance.Publish(new UIShowEvent { ViewType = viewType });
        
        return true;
    }
    
    public bool Hide<T>() where T : BaseView
    {
        Type viewType = typeof(T);
        
        if (!_views.TryGetValue(viewType, out BaseView view) || 
            view == null || !view.gameObject.activeInHierarchy)
        {
            return false;
        }
        
        view.gameObject.SetActive(false);
        
        EventManager.Instance.Publish(new UIHideEvent { ViewType = viewType });
        
        return true;
    }
    
    public bool Destroy<T>() where T : BaseView
    {
        Type viewType = typeof(T);
        
        if (_views.TryGetValue(viewType, out BaseView view) && view != null)
        {
            GameObject.Destroy(view.gameObject);
            _views.Remove(viewType);
            return true;
        }
        
        return false;
    }
    
    public void HideAll()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
    
    public void ShowAll()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }
    
    public void Update()
    {
    }
    
    public void Cleanup()
    {
        foreach (var kvp in _views)
        {
            if (kvp.Value != null)
            {
                GameObject.Destroy(kvp.Value.gameObject);
            }
        }
        _views.Clear();
        
        _uiCanvas = null;
        _canvasGroup = null;
    }
    
    #endregion
    
    #region UI层级和组件管理
    
    private void InitializeUICanvas()
    {
        GameObject canvasObj = GameObject.Find("UICanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("UICanvas");
            _uiCanvas = canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _uiCanvas.sortingOrder = 0;
        }
        else
        {
            _uiCanvas = canvasObj.GetComponent<Canvas>();
        }
        
        _canvasGroup = _uiCanvas.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = _uiCanvas.gameObject.AddComponent<CanvasGroup>();
        }
        
        GameObject.DontDestroyOnLoad(_uiCanvas.gameObject);
    }
    
    private BaseView GetOrCreateView<T>() where T : BaseView
    {
        Type viewType = typeof(T);
        
        if (_views.TryGetValue(viewType, out BaseView existingView) && existingView != null)
        {
            return existingView;
        }
        
        string prefabPath = $"Prefabs/UI/{viewType.Name}";
        GameObject prefab = ResourceManager.Instance.Load<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] 找不到UI预制体: {prefabPath}");
            return null;
        }
        
        GameObject viewObj = GameObject.Instantiate(prefab, _uiCanvas.transform);
        BaseView view = viewObj.GetComponent<T>();
        
        if (view == null)
        {
            Debug.LogError($"[UIManager] 预制体上没有找到{viewType.Name}组件");
            GameObject.Destroy(viewObj);
            return null;
        }
        
        return view;
    }
    
    private void SetViewLayer(BaseView view, UILayer layer)
    {
        if (view != null)
        {
            view.transform.SetAsLastSibling();
            
            Canvas viewCanvas = view.GetComponent<Canvas>();
            if (viewCanvas != null)
            {
                viewCanvas.sortingOrder = (int)layer;
            }
        }
    }
    
    #endregion
}

#region UI事件定义

// UI显示事件，用于系统间通信
public class UIShowEvent : IEvent
{
    public Type ViewType { get; set; }
}

// UI隐藏事件，用于系统间通信
public class UIHideEvent : IEvent
{
    public Type ViewType { get; set; }
}

#endregion 