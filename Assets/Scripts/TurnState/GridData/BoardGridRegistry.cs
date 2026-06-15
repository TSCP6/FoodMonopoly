using System;
using System.Collections.Generic;
using UnityEngine;

//登记地图格子类
public class BoardGridRegistry : MonoBehaviour
{
    [SerializeField] private Transform boardRoot; //地图根物体
    [SerializeField] private List<BoardGridView> gridViews = new List<BoardGridView>();

    public int Count => gridViews.Count; //格子数

    private void Awake()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (boardRoot == null)
        {
            boardRoot = transform;
        }

        gridViews.Clear();

        BoardGridView[] views = boardRoot.GetComponentsInChildren<BoardGridView>(true);
        //排序格子
        Array.Sort(views, (left, right) => left.GridIndex.CompareTo(right.GridIndex));

        for (int i = 0; i < views.Length; i++)
        {
            views[i].SyncFromScene(); //确保数据与场景一致
            gridViews.Add(views[i]);
        }
    }

    //根据索引获取格子视图
    public BoardGridView GetView(int index)
    {
        if (gridViews.Count == 0)
        {
            return null;
        }

        int safeIndex = index % gridViews.Count;
        if (safeIndex < 0)
        {
            safeIndex += gridViews.Count;
        }

        return gridViews[safeIndex];
    }
}