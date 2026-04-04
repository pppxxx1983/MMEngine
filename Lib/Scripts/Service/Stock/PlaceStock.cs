using System;
using System.Collections.Generic;
using SP;
using UnityEngine;

public class PlaceStock : Service
{
    [Input]
    public TransformListVar dataSource ;
    [Input]
    public StockVar placeStocks;

    public override void Enter()
    {

        if (!dataSource.ValidateAndLog(this) || !placeStocks.ValidateAndLog(this))
        {
            NextService();
            return;
        }

        List<Transform> list = dataSource.Get();
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("PlaceStock service returned a null list",this);
            NextService();
            return;
        }

        var targetStock = placeStocks.Get();
        if (targetStock == null)
        {
            Debug.LogWarning("PlaceStock service returned a null target stock",this);
            NextService();
            return;
        }
        for (int i = 0; i < list.Count; i++)
        {
            Transform current = list[i];
            if (current == null)
                continue;
        
            targetStock.StockIn(current, false);
        }
    
        NextService();
    }
    //
    // private static bool IsSupportedMode(StockSourceMode mode)
    // {
    //     return mode == StockSourceMode.Drag || mode == StockSourceMode.Global;
    // }
}

