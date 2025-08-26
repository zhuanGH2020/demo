using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Vector3 _offset = new Vector3(0, 5, -10);

    void Start()
    {
        
    }

    void Update()
    {
        if (PlayerMain.Instance != null)
        {
            Camera.main.transform.position = PlayerMain.Instance.transform.position + _offset;
        }
    }
}
