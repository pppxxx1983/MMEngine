using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

namespace SP
{
    
    public class StockToProgressTirgger : Service
    {

        [Header("碰撞对象")] 
        [Input] public TriggerVar trigger1;
        [Input] public TriggerVar trigger2;

        [Header("操作堆")]
        [Input]
        public StockVar fromStock;
        [Input]
        public ProgressBarVar toProgress;
        
        
        public override void Enter()
        {
            if (!trigger1.ValidateAndLog(this) || !trigger2.ValidateAndLog(this))
            {
                Debug.LogError($"[{GetType().Name}],操作对象 未设置");
                NextService();
                return;
            }
        }

        public override void Update()
        {
            Trigger _trigger1 =  trigger1.Get();
            Trigger _trigger2 =  trigger2.Get();
            Stock _socket = fromStock.Get();
            ProgressBar _toProgress =  toProgress.Get();
            if (_trigger1 == null || _trigger2 == null || _socket == null || _toProgress == null)
            {
                NextService();
                return;
            }
            if (_trigger1.IsTrigger(_trigger2))
            {
                while (_socket.Count() > 0)
                {
                    _socket.TakeAwayWorld(_toProgress.transform.position,true,  (transform) =>
                    {
                        var Item = transform.gameObject.GetComponent<Item>();
                        _toProgress.AddProcess(Item.value);
                        Root.Instance.resourceCenter.Release(transform.gameObject);
                    });
                }
            }

            if (_toProgress.IsMax())
            {
                NextService();
            }
        }
    }
}
    
