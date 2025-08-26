using UnityEngine;

// Shop数据模型 - 负责ui_shop数据管理
public class ShopModel
{
    private static ShopModel _instance;
    public static ShopModel Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ShopModel();
            }
            return _instance;
        }
    }

    private ShopModel()
    {
        
    }

    private void NotifyDataChanged()
    {
        
    }
}
