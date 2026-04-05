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
        [Header("Stock Container")]
        [Tooltip("鍫嗘斁鐨勮鏁板拰鍒楁暟")]
        public Vector2Int rowColumn = new Vector2Int(3, 2);

        [Tooltip("Cell size for stacked items")]
        public Vector3 cellSize = new Vector3(0.5f, 0.2f, 0.5f);

        [Tooltip("鍫嗘斁鐨勫垵濮嬮珮搴﹁捣鐐?(鍦ㄨ繖涓珮搴︿箣涓婂紑濮嬪爢鍙?")]
        public float startOffsetY = 0f;

        private List<Transform> _itemsOfStocked = new List<Transform>(); 
        private int _flyingCount = 0; 
        
        [ResourceName(ResourceCategory.Prefab)]
        public List<String> addTypes = new List<String>();
        
        [Tooltip("Maximum stock capacity")]
        public int capacity = 40;

        [Header("鍔ㄧ敾閰嶇疆")]
        [Tooltip("Animation style when item enters stock")]
        public int animationStyle = 0;

        [Tooltip("鐗╁搧鍫嗘斁鐨勯€熷害锛岀背姣忕")]
        public float stockSpeed = 0.5f;

        [Tooltip("搴曞眰鐗╁搧琚娊璧板悗锛屼笂鏂圭墿鍝佹帀钀借ˉ浣嶇殑鍔ㄧ敾鏃堕暱")]
        public float rearrangeDuration = 0.2f;

        public Ease easeFunction = Ease.OutCubic;

        public System.Action OnStockCountChanged;

        // 銆愭柊澧炪€戯細鑴忔爣璁帮紝鐢ㄤ簬浼樺寲鍚屼竴甯у唴鐨勫娆′笅钀藉姩鐢?
        private bool _needRearrange = false;

        // 銆愭柊澧炪€戯細鍦ㄨ繖涓€甯х殑鎵€鏈夐€昏緫锛堟嬁鍙栥€佹斁鍏ワ級閮界粨鏉熷悗锛岀粺涓€鎵ц涓€娆′笅钀芥帓鐗?
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
                        
                        // 銆愭牳蹇冧慨鏀广€戯細涓嶅啀绔嬪埢瑙﹀彂鎺掔増锛岃€屾槸鎵撲釜鈥滈渶瑕佹帓鐗堚€濈殑鏍囪
                        RearrangeStock();
                        
                        OnStockCountChanged?.Invoke(); 
                        return item;
                    }
                }
                index--;
            }
            return null;
        }

        // 鎵撲笂鑴忔爣璁帮紝浜ょ粰 LateUpdate 缁熶竴澶勭悊
        private void RearrangeStock()
        {
            _needRearrange = true;
        }

        // 瀹為檯鎵ц閲嶆柊鎺掔増鐨勬柟娉?
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
        [Header("Editor Preview")]
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
