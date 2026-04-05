using UnityEngine;
using DG.Tweening;
using SP; 

namespace SP
{
    public class PlayerStockManager : MonoBehaviour
    {
        [Header("排版设置")]
        [Tooltip("背包之间的最小间隙距离")]
        public float padding = 0.1f;
        
        [Tooltip("【重要】第一个物品的边缘距离玩家的间距 (不再是中心点，而是精确的边缘测距)")]
        public float startOffsetZ = -0.2f; 
        
        [Tooltip("Z轴排队方向：-1 表示往身后排，1 表示往身前排")]
        public float zDirection = -1f;

        [Tooltip("追尾滑动动画的耗时")]
        public float moveDuration = 0.2f;

        // 缓存所有的子节点背包
        private Stock[] _stocks;
        
        // 记录上一帧每个背包的数量，用于比对
        private int[] _lastCounts;

        private void Start()
        {
            // 自动获取所有子节点背包
            // 【注意】：Hierarchy 面板中，谁在上面，谁的排队优先级就高！
            _stocks = GetComponentsInChildren<Stock>();
            
            _lastCounts = new int[_stocks.Length];
            for (int i = 0; i < _stocks.Length; i++)
            {
                _lastCounts[i] = -1; 
            }
        }

        private void Update()
        {
            if (_stocks == null || _stocks.Length == 0) return;

            bool needRecalculate = false;

            for (int i = 0; i < _stocks.Length; i++)
            {
                int currentCount = _stocks[i].Count();
                if (currentCount != _lastCounts[i])
                {
                    _lastCounts[i] = currentCount;
                    needRecalculate = true;
                }
            }

            if (needRecalculate)
            {
                RecalculatePositions();
            }
        }

        /// <summary>
        /// 核心方法：基于真实占用深度，动态平滑排队
        /// </summary>
        private void RecalculatePositions()
        {
            // 起跑线：代表“物品的真实物理边缘”
            float currentZ = startOffsetZ;

            for (int i = 0; i < _stocks.Length; i++)
            {
                Stock stock = _stocks[i];
                int count = stock.Count();

                // 1. 如果背包空了，就把它藏在 currentZ 待命，不占用后面的厚度
                if (count == 0)
                {
                    stock.transform.DOLocalMoveZ(currentZ, moveDuration).SetEase(Ease.OutQuad);
                    continue;
                }

                // 2. 如果背包有物品，计算它的自适应偏移
                float depth = CalculateZDepth(stock, count);
                float cellSizeZ = stock.cellSize.z;
                float targetZ = 0f;

                if (zDirection < 0)
                {
                    // 往后排 (-Z 方向)
                    // 动态减去物品半径，让物品的“最前端边缘”精确对齐 currentZ 起跑线
                    targetZ = currentZ - (depth - cellSizeZ / 2f);
                    
                    stock.transform.DOLocalMoveZ(targetZ, moveDuration).SetEase(Ease.OutQuad);
                    
                    // 把下一个背包的起跑线往后推 (本包总深度 + 间距)
                    currentZ = currentZ - depth - padding;
                }
                else
                {
                    // 往前排 (+Z 方向)
                    // 动态加上物品半径，让物品的“最后端边缘”精确对齐 currentZ
                    targetZ = currentZ + (cellSizeZ / 2f);
                    
                    stock.transform.DOLocalMoveZ(targetZ, moveDuration).SetEase(Ease.OutQuad);
                    
                    // 给下一个背包留出的新起点
                    currentZ = currentZ + depth + padding;
                }
            }
        }

        /// <summary>
        /// 逆推算法：根据 Stock 里的行列配置和当前数量，计算它占用的真实 Z 轴长度
        /// </summary>
        private float CalculateZDepth(Stock stock, int totalCount)
        {
            if (totalCount <= 0) return 0f;

            // 【核心修复】：防止多层堆叠时，上层物品的 column 归零导致整个背包深度坍缩
            int maxItemsPerLayer = stock.rowColumn.x * stock.rowColumn.y;
            int maxColumn = 0;
            
            // 如果数量超过（或等于）一层，说明Z轴（前后方向）已经铺满了，物理深度就是最大列数
            if (totalCount >= maxItemsPerLayer)
            {
                maxColumn = stock.rowColumn.x - 1;
            }
            else
            {
                // 如果还没铺满底层，就看目前堆到了第几列
                maxColumn = (totalCount - 1) / stock.rowColumn.y;
            }
            
            // 真实占用深度 = (最大列索引 + 1) * 每个格子的Z长度
            return (maxColumn + 1) * stock.cellSize.z;
        }
    }
}
