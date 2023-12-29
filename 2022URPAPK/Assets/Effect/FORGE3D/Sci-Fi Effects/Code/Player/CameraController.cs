using System.Collections;
using System.Collections.Generic;
using UnityEngine;
///using FORGE3D;

/// <summary>
/// ��������ƶ�ʱ�������������ת��
/// ��������ƶ�ʱ���������̧ͷ��ͷ
/// �ýű����ص����Camera��
/// Todo������ƽ����ֵ���߶���Ч��
/// </summary>
public class CameraController : MonoBehaviour
{
    public GameObject target;

    void Start()
    {
        ToHideCursor();
    }
    /// <summary>
    /// �������
    /// </summary>
    void ToHideCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
       // Cursor.visible = false;
    }
    /// <summary>
    /// ��ʾ���
    /// </summary>
    void ToShowCursor()
    {
      //  Cursor.visible = true;
    }

    void Update()
    {
        //��������
        float rotateX = Input.GetAxis("Mouse X") * 20.0f * Time.deltaTime;
        float rotateY = Input.GetAxis("Mouse Y") * 20.0f * Time.deltaTime;

        //// ��������ת
        //if (Input.GetMouseButton(0))
        //{
        //    Vector3 eulerAngles = transform.eulerAngles;

        //    eulerAngles.y += rotateX;
        //    eulerAngles.x -= rotateY;

        //    transform.eulerAngles = eulerAngles;
        //}


        // ����ͷ��ת
        if (target!=null)
        {
            float distance = Vector3.Distance(target.transform.position, transform.position);

            transform.position += transform.right * -rotateX + transform.up * -rotateY;

            transform.LookAt(target.transform);

            transform.position = target.transform.position + -transform.forward * distance;
        }
    }
}
