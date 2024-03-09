using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>카메라 움직임 및 Ray 관리</summary>
public class CameraController : MonoBehaviour
{
    public PlayerManager player;
    public float sensitivity = 100f;
    public float clampAngle = 85f;
    public float maxDistance = 5f;

    private float verticalRotation;
    private float horizontalRotation;
    private int layerMask;
    private Ray ray;
    private RaycastHit rayHit;
    private GameObject selectedPanel;


    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = player.transform.eulerAngles.y;

        layerMask = 1 << LayerMask.NameToLayer("ColorPanel");
    }

    private void Update()
    {
        Look();
        //Debug.DrawRay(transform.position, transform.forward * 5f, Color.red);    // 사용자 시야 Ray 그림
        ray = GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));   // 화면의 가운데 기준으로 ray 생성

        // hit한 Object가 ColorPanel일 경우
        if (Physics.Raycast(ray, out rayHit, maxDistance, layerMask))
        {
            Debug.DrawLine(ray.origin, rayHit.point, Color.green);
            selectedPanel = rayHit.transform.GetChild(0).gameObject;
            selectedPanel.SetActive(true);
            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("색판 뒤집음");
                ClientSend.PlayerFlip(rayHit.collider.gameObject.GetComponent<ColorPanel>());
            }
        }
        else
        {
            if (selectedPanel != null)
                selectedPanel.SetActive(false);
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red);
        }
    }

    private void Look()
    {
        float mouseVertical = -Input.GetAxis("Mouse Y");
        float mouseHorizontal = Input.GetAxis("Mouse X");

        verticalRotation += mouseVertical * sensitivity * Time.deltaTime;
        horizontalRotation += mouseHorizontal * sensitivity * Time.deltaTime;

        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);  // vertical 움직임 제한

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        player.transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }
}
