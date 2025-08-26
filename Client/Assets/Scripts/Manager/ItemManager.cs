using System.Collections.Generic;
using UnityEngine;

// 物品数据类，包装物品类型和配置信息
public class Item
{
    public ItemType Type { get; private set; }
    public ConfigReader Csv { get; private set; }

    public Item(ItemType type, ConfigReader csv)
    {
        Type = type;
        Csv = csv;
    }

    public bool IsItem()
    {
        return Type == ItemType.Item;
    }

    public bool IsEquip()
    {
        return Type == ItemType.Equip;
    }
}

// 物品管理器，统一管理物品查询和缓存
public class ItemManager
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance ??= new ItemManager();

    private Dictionary<int, Item> _cache = new Dictionary<int, Item>();
    private ConfigReader _reader;

    public Item GetItem(int id)
    {
        if (_cache.ContainsKey(id))
            return _cache[id];

        if (_reader == null)
        {
            _reader = ConfigManager.Instance.GetReader("Item");
        }

        if (_reader == null || !_reader.HasKey(id))
            return null;

        ItemType type = _reader.GetValue<ItemType>(id, "Type", ItemType.None);
        Item item = new Item(type, _reader);
        _cache[id] = item;

        return item;
    }
}
