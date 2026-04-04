using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SP
{
    public class Stock : MonoBehaviour,IMMVarTarget
    {
        [Header("摆放物品的容器")]
        [Tooltip("堆放的行数和列数")]
        public Vector2Int rowColumn = new Vector2Int(3, 2);

        [Tooltip("堆放物品格子的大小")]
        public Vector3 cellSize = new Vector3(0.5f, 0.2f, 0.5f);

        [Tooltip("堆放的初始高度起点 (在这个高度之上开始堆叠)")]
        public float startOffsetY = 0f;

        private List<Transform> _itemsOfStocked = new List<Transform>(); 
        private int _flyingCount = 0; 
        
        [ResourceName(ResourceCategory.Prefab)]
        public List<String> addTypes = new List<String>();
        
        [Tooltip("能够堆放的最大容量")]
        public int capacity = 40;

        [Header("动画配置")]
        [Tooltip("物品放入背包的动画样式")]
        public int animationStyle = 0;

        [Tooltip("物品堆放的速度，米每秒")]
        public float stockSpeed = 0.5f;

        [Tooltip("底层物品被抽走后，上方物品掉落补位的动画时长")]
        public float rearrangeDuration = 0.2f;

        public Ease easeFunction = Ease.OutCubic;

        public System.Action OnStockCountChanged;

        // 【新增】：脏标记，用于优化同一帧内的多次下落动画
        private bool _needRearrange = false;

        // 【新增】：在这一帧的所有逻辑（拿取、放入）都结束后，统一执行一次下落排版
        private void LateUpdate()
        {
            if (_needRearrange)
            {
                _needRearrange = false;
                ExecuteRearrange();
            }
        }

        public bool IsFull()
        {
            return Count() >= capacity;
        }

        public int Count()
        {
            return CompleteCount() + _flyingCount;
        }

        public int CompleteCount()
        {
            return _itemsOfStocked.Count;
        }

        public Transform PeekLastItem()
        {
            if (CompleteCount() == 0) return null;
            return _itemsOfStocked[_itemsOfStocked.Count - 1];
        }

        public float GetCurrentZDepth()
        {
            int total = Count();
            if (total <= 0) return 0f; 
            
            int index = total - 1;
            int layer = index / (rowColumn.x * rowColumn.y);
            int left = index - layer * rowColumn.x * rowColumn.y;
            int column = left / rowColumn.y;
            
            return (column + 1) * cellSize.z; 
        }

        private bool IsAcceptableType(GameObject itemObj)
        {
            if (addTypes == null || addTypes.Count == 0) return true;
            
            ResourceInstanceMarker marker = itemObj.GetComponent<ResourceInstanceMarker>();
            if (marker != null && !string.IsNullOrEmpty(marker.resourceName))
            {
                return addTypes.Contains(marker.resourceName);
            }

            string rawName = itemObj.name.Replace("(Clone)", "").Trim();
            return addTypes.Contains(rawName);
        }

        private bool IsAcceptableTypeString(string type)
        {
            if (addTypes == null || addTypes.Count == 0) return true;
            return addTypes.Contains(type);
        }

        public Transform Pop(String type="")
        {
            if (_itemsOfStocked.Count == 0) return null;

            if (!string.IsNullOrEmpty(type) && !IsAcceptableTypeString(type)) return null;

            var index = _itemsOfStocked.Count - 1;
            
            if (string.IsNullOrEmpty(type))
            {
                var item = _itemsOfStocked[index];
                _itemsOfStocked.RemoveAt(index);
                item.SetParent(null);
                
                OnStockCountChanged?.Invoke(); 
                return item;
            }
            
            while (index >= 0)
            {
                var item = _itemsOfStocked[index];
                if (item != null)
                {
                    bool isMatch = false;
                    ResourceInstanceMarker marker = item.GetComponent<ResourceInstanceMarker>();
                    
                    if (marker != null && marker.resourceName == type) isMatch = true;
                    else if (item.name.Replace("(Clone)", "").Trim() == type) isMatch = true;

                    if (isMatch)
                    {
                        _itemsOfStocked.RemoveAt(index);
                        item.SetParent(null);
                        
                        // 【核心修改】：不再立刻触发排版，而是打个“需要排版”的标记
                        RearrangeStock();
                        
                        OnStockCountChanged?.Invoke(); 
                        return item;
                    }
                }
                index--;
            }
            return null;
        }

        // 打上脏标记，交给 LateUpdate 统一处理
        private void RearrangeStock()
        {
            _needRearrange = true;
        }

        // 实际执行重新排版的方法
        private void ExecuteRearrange()
        {
            for (int i = 0; i < _itemsOfStocked.Count; i++)
            {
                Transform item = _itemsOfStocked[i];
                if (item != null)
                {
                    Vector3 targetPos = GetAvailableStockPosition(i);
                    
                    if (Vector3.Distance(item.localPosition, targetPos) > 0.01f)
                    {
                        item.DOKill(); 
                        item.DOLocalMove(targetPos, rearrangeDuration).SetEase(Ease.OutQuad);
                    }
                }
            }
        }

        public Vector3 GetAvailableStockPosition(int totalCount)
        {
            var left = totalCount;
            int layer = left / (rowColumn.x * rowColumn.y);
            left = left - layer * rowColumn.x * rowColumn.y;
            int column = left / rowColumn.y;
            int row = left - column * rowColumn.y;
            
            float y = startOffsetY + cellSize.y * layer; 
            float z = cellSize.z * column;
            float x = cellSize.x * row;
            return new Vector3(x, y, z);
        }

        public bool StockIn(Transform item, bool useDefaultAnimation)
        {
            if (item == null || !IsAcceptableType(item.gameObject)) return false;
            if (IsFull()) return false;
            
            var destination = GetAvailableStockPosition(Count());
            item.SetParent(transform);
            
            if (useDefaultAnimation)
            {
                _flyingCount++; 
                item.DOLocalRotate(Vector3.zero, 0.5f);
                item.DOLocalJump(destination, 2f, 1, stockSpeed, false).SetSpeedBased(true).SetEase(easeFunction).OnComplete(() =>
                {
                    _flyingCount--; 
                    item.localPosition = GetAvailableStockPosition(CompleteCount()); 
                    _itemsOfStocked.Add(item);
                });
            }
            else
            {
                _itemsOfStocked.Add(item);
                item.localRotation = Quaternion.identity;
                item.localPosition = destination;
            }
            
            OnStockCountChanged?.Invoke(); 
            return true;
        }

        public Transform TakeAwayLocal(Transform destinationParent, Vector3 localPositionOfDestination, bool useDefaultAnimation, System.Action<Transform> onTakeAwayComplete)
        {
            var item = Pop();
            if (item == null) return null;
            item.SetParent(destinationParent);
            if (useDefaultAnimation)
            {
                item.DOLocalRotate(Vector3.zero, 0.5f);
                item.DOLocalJump(localPositionOfDestination, 2f, 1, stockSpeed, false).SetSpeedBased(true).SetEase(easeFunction).OnComplete(() =>
                {
                    onTakeAwayComplete?.Invoke(item);
                });
            }
            else
            {
                item.localPosition = localPositionOfDestination;
                onTakeAwayComplete?.Invoke(item);
            }
            return item;
        }

        public Transform TakeAwayWorld(Vector3 worldPosition, bool useDefaultAnimation, System.Action<Transform> onTakeAwayComplete)
        {
            var item = Pop();
            if (item == null) return null;
            if (useDefaultAnimation)
            {
                item.DOLocalRotate(Vector3.zero, 0.5f);
                item.DOJump(worldPosition, 2f, 1, stockSpeed, false).SetSpeedBased(true).SetEase(easeFunction).OnComplete(() =>
                {
                    onTakeAwayComplete?.Invoke(item);
                });
            }
            else
            {
                item.position = worldPosition;
                onTakeAwayComplete?.Invoke(item);
            }
            return item;
        }

#if UNITY_EDITOR
        [Header("编辑器演示")]
        [Space]
        public UnityEngine.Object prefabObj;
        private UnityEngine.Object lastObj;
        public bool alwaysShow = false;
        private List<UnityEngine.Object> showObjs = new();
        public bool updateShowObj = false;

        private void Draw()
        {
            var originGizmosColor = Gizmos.color;
            var originGizmosMatrix = Gizmos.matrix;
            Gizmos.color = Color.blue;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            Vector3 vertexLocal = new Vector3(
                (rowColumn.y - 1) * cellSize.x,
                Mathf.FloorToInt(((float)capacity - 1) / (rowColumn.x * rowColumn.y)) * cellSize.y,
                (rowColumn.x - 1) * cellSize.z
            );
            
            Vector3 center = vertexLocal / 2f;
            center.y += startOffsetY;
            Gizmos.DrawWireCube(center, vertexLocal);
            
            for (int i = 0; i < capacity; i++)
            {
                Vector3 pos = GetAvailableStockPosition(i);
                Gizmos.DrawSphere(pos, 0.05f);

                if (!EditorApplication.isPlaying)
                {
                    UnityEngine.Object t = GetShowObj(i);
                    if (t != null) (t as GameObject).transform.localPosition = pos;
                }
            }
            Gizmos.color = originGizmosColor;
            Gizmos.matrix = originGizmosMatrix;
        }

        private void OnDrawGizmos()
        {
            if (Selection.transforms != null && Selection.transforms.Length > 0 && Selection.transforms[0] == transform || alwaysShow)
            {
                int showCount = showObjs.Count;
                if (showCount > 0)
                {
                    if (prefabObj == null || lastObj != prefabObj || showCount > capacity || transform.childCount != showCount || updateShowObj)
                    {
                        for (int i = showCount - 1; i >= 0; i--)
                        {
                            if (showObjs[i] != null) DestroyImmediate(showObjs[i]);
                        }
                        showObjs.Clear();
                        GC.Collect();
                        updateShowObj = false;
                    }
                }
                Draw();
            }
            else
            {
                int showCount = showObjs.Count;
                if (showCount > 0)
                {
                    for (int i = showCount - 1; i >= 0; i--)
                    {
                        if (showObjs[i] != null) DestroyImmediate(showObjs[i]);
                    }
                    showObjs.Clear();
                    GC.Collect();
                }
            }
        }

        private UnityEngine.Object GetShowObj(int i)
        {
            if (prefabObj == null)
            {
                if (showObjs.Count > 0) showObjs.Clear();
                lastObj = null;
                return null;
            }
            if (showObjs.Count > i) return showObjs[i];
            UnityEngine.Object t = PrefabUtility.InstantiatePrefab(prefabObj);
            lastObj = prefabObj;
            (t as GameObject).transform.SetParent(transform, false);
            showObjs.Add(t);
            return t;
        }
#endif
    }
}