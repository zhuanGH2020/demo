using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // 初始化时钟系统
        ClockModel.Instance.InitializeTime();
    }

    // Update is called once per frame
    void Update()
    {
        // 更新时钟系统
        ClockModel.Instance.UpdateClock();
    }
}
