using System.Collections;
using System.Collections.Generic;
using UnityEngine;
///using FORGE3D;

/// <summary>
/// 鼠标左右移动时，控制相机左右转动
/// 鼠标上下移动时，控制相机抬头低头
/// 该脚本挂载到相机Camera上
/// Todo：加入平滑插值或者动画效果
/// </summary>
public class CameraController : MonoBehaviour
{
    public GameObject target;

    void Start()
    {
        ToHideCursor();
    }
    /// <summary>
    /// 隐藏鼠标
    /// </summary>
    void ToHideCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
       // Cursor.visible = false;
    }
    /// <summary>
    /// 显示鼠标
    /// </summary>
    void ToShowCursor()
    {
      //  Cursor.visible = true;
    }

    void Update()
    {
        //接收输入
        float rotateX = Input.GetAxis("Mouse X") * 20.0f * Time.deltaTime;
        float rotateY = Input.GetAxis("Mouse Y") * 20.0f * Time.deltaTime;

        //// 绕自身旋转
        //if (Input.GetMouseButton(0))
        //{
        //    Vector3 eulerAngles = transform.eulerAngles;

        //    eulerAngles.y += rotateX;
        //    eulerAngles.x -= rotateY;

        //    transform.eulerAngles = eulerAngles;
        //}


        // 绕喷头旋转
        if (target!=null)
        {
            float distance = Vector3.Distance(target.transform.position, transform.position);

            transform.position += transform.right * -rotateX + transform.up * -rotateY;

            transform.LookAt(target.transform);

            transform.position = target.transform.position + -transform.forward * distance;
        }
    }
}
