using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; 

namespace SP
{
    public class StockToStockTirgger : Service
    {
        [Header("Section 1")]
        public List<Trigger> trigger1 = new List<Trigger>();
        
        public List<Trigger> trigger2 = new List<Trigger>();

        [Header("Section 2")]
        public Stock fromStock;
        public Stock toStock;

        [Header("Section 3")]
        [Tooltip("Info 1")]
        public bool transferAllAtOnce = false; 

        [Tooltip("Info 2")]
        public float transferInterval = 0.15f; 

        // 鏄惁姝ｅ湪鎵ц杞Щ锛堥槻姝㈤噸澶嶈Е鍙戯級
        private bool _isTransferring = false;
        private void OnEnable()
        {
            // 鍒濆鍖栬浆绉荤姸鎬?
            _isTransferring = false; 

            if (trigger1 == null || trigger2 == null || trigger1.Count == 0 || trigger2.Count == 0 || fromStock == null ||  toStock == null)
            {
                Debug.LogError($"[{GetType().Name}] Missing required references.");
                Next();
                return;
            }
        }

        private void Update()
        {
            // 姝ｅ湪杞Щ鏃讹紝涓嶉噸澶嶆墽琛?
            if (_isTransferring) return;

            if (Tools.IsTirgger(trigger1, trigger2))
            {
                // 鏍囪寮€濮嬭浆绉伙紝闃叉閲嶅瑙﹀彂
                _isTransferring = true;

                // 鏍规嵁璁剧疆閫夋嫨杞Щ鏂瑰紡
                if (transferAllAtOnce)
                {
                    TransferAllInstantly();
                }
                else
                {
                    TransferNextItem();
                }
            }
        }

        // ==========================================
        // 鏂瑰紡A锛氫竴娆℃€х灛闂磋浆绉绘墍鏈夌墿鍝侊紙鏃犻棿闅旓級
        // ==========================================
        private void TransferAllInstantly()
        {
            int count = fromStock.CompleteCount();
            for (int k = 0; k < count; k++)
            {
                // 鐩爣搴撳瓨宸叉弧锛屽仠姝㈣浆绉?
                if (toStock.IsFull()) break;

                Transform item = null;

                // 鏍规嵁鐩爣搴撳瓨鍏佽鐨勭被鍨嬭幏鍙栫墿鍝?
                if (toStock.addTypes != null && toStock.addTypes.Count > 0)
                {
                    foreach (string neededType in toStock.addTypes)
                    {
                        item = fromStock.Pop(neededType);
                        if (item != null) break;
                    }
                }
                else
                {
                    item = fromStock.Pop();
                }

                // 鏃犵墿鍝佸彲鍙栵紝鍋滄杞Щ
                if (item == null) break;

                // 灏濊瘯灏嗙墿鍝佹斁鍏ョ洰鏍囧簱瀛?
                if (!toStock.StockIn(item, true))
                {
                    // 鏀惧叆澶辫触锛岀墿鍝佹斁鍥炲師搴撳瓨
                    fromStock.StockIn(item, false);
                    break;
                }
            }
            
            // 杞Щ瀹屾垚锛岄噸缃姸鎬?
            FinishTransfer();
        }

        // ==========================================
        // 鏂瑰紡B锛氶€愪釜闂撮殧杞Щ鐗╁搧锛堝甫寤惰繜鏁堟灉锛?
        // ==========================================
        private void TransferNextItem()
        {
            // 瑙﹀彂鏉′欢涓嶆弧瓒筹紝鐩存帴缁撴潫杞Щ
            if (!Tools.IsTirgger(trigger1, trigger2))
            {
                FinishTransfer();
                return;
            }

            // 1. 婧愬簱瀛樼┖ 鎴?鐩爣搴撳瓨婊★紝缁撴潫杞Щ
            if (fromStock.CompleteCount() == 0 || toStock.IsFull())
            {
                FinishTransfer();
                return;
            }

            Transform item = null;

            // 2. 鏍规嵁鐩爣搴撳瓨绫诲瀷绛涢€夌墿鍝?
            if (toStock.addTypes != null && toStock.addTypes.Count > 0)
            {
                foreach (string neededType in toStock.addTypes)
                {
                    item = fromStock.Pop(neededType);
                    if (item != null) break; // 鑾峰彇鍒板搴旂被鍨嬬墿鍝侊紝鍋滄鏌ユ壘
                }
            }
            else
            {
                // 鏃犵被鍨嬮檺鍒讹紝鐩存帴鍙栧嚭绗竴涓墿鍝?
                item = fromStock.Pop();
            }

            // 3. 鏈幏鍙栧埌鏈夋晥鐗╁搧锛岀粨鏉熻浆绉?
            if (item == null)
            {
                FinishTransfer();
                return;
            }

            // 4. 灏濊瘯灏嗙墿鍝佹斁鍏ョ洰鏍囧簱瀛?
            if (!toStock.StockIn(item, true))
            {
                // 鏀惧叆澶辫触锛岀墿鍝侀€€鍥炲師搴撳瓨骞剁粨鏉熻浆绉?
                fromStock.StockIn(item, false);
                FinishTransfer();
                return;
            }

            // 5. 寤惰繜鍚庣户缁浆绉讳笅涓€涓墿鍝?
            DOVirtual.DelayedCall(transferInterval, TransferNextItem);
        }

        // 杞Щ瀹屾垚锛氶噸缃姸鎬佸苟鎵ц涓嬩竴涓湇鍔?
        private void FinishTransfer()
        {
            _isTransferring = false; // 缁撴潫杞Щ鐘舵€?
            Next();           // 鎵ц鍚庣画閫昏緫
        }
        

    }
}





