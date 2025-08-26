using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ShopView - ui_shopUI视图
public class ShopView : BaseView
{
    public static string PrefabPath => "Prefabs/UI/ui_shop";

    private void Start()
    {
        Initialize();
        SubscribeEvents();
    }

    private void Initialize()
    {
        
    }

    private void SubscribeEvents()
    {
        // 订阅数据变化事件
    }

    private void OnDataChanged()
    {
        
    }
}
