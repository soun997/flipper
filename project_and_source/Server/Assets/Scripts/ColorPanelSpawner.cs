﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPanelSpawner : MonoBehaviour
{
    public static ColorPanelSpawner instance;
    public List<ColorPanel> colorPanels;
    public GameObject colorPanelPrefab;

    private Vector3 initPosition;
    private Vector3 nextPosition;
    private float distance;
    private int panelCount;

    public void Awake()
    {
        // 싱글톤 객체 생성
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("싱글톤 인스턴스가 존재하므로 오브젝트를 Destroy합니다.");
            Destroy(this);
        }
    }

    private void Start()
    {
        initPosition = nextPosition = new Vector3(-10f, 0.05f, 10f);
        distance = 2f;
        panelCount = 1;

        for (int i = 0; i <= 10; i++)
        {
            for (int j = 0; j <= 10; j++)
            {
                GameObject colorPanel = Instantiate(colorPanelPrefab, nextPosition, Quaternion.identity);
                colorPanel.GetComponent<ColorPanel>().panelID = panelCount++;
                colorPanels.Add(colorPanel.GetComponent<ColorPanel>());
                colorPanel.transform.parent = GameObject.Find("World").transform;
                nextPosition.x += distance;
            }
            initPosition.z -= distance;
            nextPosition = initPosition;
        }
    }
}