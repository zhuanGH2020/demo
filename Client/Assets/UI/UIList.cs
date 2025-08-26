// Last modified: 2024-12-19 14:30:15
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI列表组件 - 支持水平和垂直排列的动态列表，基于模板item自动创建和管理子项布局
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/UIList")]
[ExecuteInEditMode]
public class UIList : MonoBehaviour
{
    [SerializeField] private GameObject _templateItem;
    
    public enum Arrangement
    {
        Horizontal,
        Vertical
    }
    
    [Header("Arrangement Settings")]
    [SerializeField] private Arrangement _arrangement = Arrangement.Horizontal;
    [SerializeField] private int _spaceWidth = 0;
    [SerializeField] private int _spaceHeight = 0;
    
    [HideInInspector]
    [SerializeField] private List<RectTransform> _listItem = new List<RectTransform>();
    
    private RectTransform _rectTrans;
    
    void Awake()
    {
        _rectTrans = this.transform as RectTransform;
        if (_rectTrans != null)
        {
            _rectTrans.pivot = new Vector2(0, 1);
        }
        if (_templateItem != null)
        {
            _templateItem.SetActive(false);
        }
        
    }
    
    // 基于模板创建新的列表项并自动布局
    public GameObject AddListItem()
    {
        if (_templateItem == null)
        {
            Debug.LogError($"{transform.parent?.name}/{gameObject.name} - TemplateItem is null");
            return null;
        }
        if(_templateItem.activeSelf)
        {
            _templateItem.SetActive(false);
        }
        
        GameObject newItem = Instantiate(_templateItem, transform, false);
        newItem.SetActive(true);
        newItem.name = _templateItem.name + "_" + _listItem.Count;
        
        RectTransform rectItem = newItem.transform as RectTransform;
        _listItem.Add(rectItem);
        
        Reposition();
        return newItem;
    }
    
    public void RemoveAll()
    {
        for (int i = _listItem.Count - 1; i >= 0; i--)
        {
            if (_listItem[i] != null)
            {
                DestroyImmediate(_listItem[i].gameObject);
            }
        }
        _listItem.Clear();
    }
    
    public int GetItemCount()
    {
        return _listItem.Count;
    }
    
    // 重新计算所有列表项的位置布局
    public void Reposition()
    {
        if (_rectTrans == null || _listItem == null || _listItem.Count == 0)
            return;
            
        Vector3 currentPos = Vector3.zero;
        
        for (int i = 0; i < _listItem.Count; i++)
        {
            RectTransform item = _listItem[i];
            if (item == null) continue;
            
            item.anchorMin = item.anchorMax = new Vector2(0, 1);
            item.pivot = new Vector2(0, 1);
            item.localPosition = currentPos;
            
            if (_arrangement == Arrangement.Horizontal)
            {
                currentPos.x += item.rect.width + _spaceWidth;
            }
            else if (_arrangement == Arrangement.Vertical)
            {
                currentPos.y -= item.rect.height + _spaceHeight;
            }
        }
    }
}