using SP;
using UnityEngine;
using DG.Tweening;
using System;

public class OpStaffWithCondition : Service
{
    [Header("Section 1")]
    public Stock bag1;
    public Stock bag2;
    public Stock outputStock;
    
    [Header("Section 2")]
    public int bag1NeededNum;
    public int bag2NeededNum;

    [Header("Section 3")]
    [Tooltip("Info 1")]
    [ResourceName(ResourceCategory.Prefab)]
    public string outputStaffName; 
    
    public Transform inputAt;     // 鍘熸枡鍚稿叆鐐癸細鐗╁搧浠庡簱瀛樿鍚歌繃鏉ョ殑鍒濆浣嶇疆
    public Transform centerAt;    // 鍚堟垚涓績鐐癸細鍘熸枡姹囪仛銆侀攢姣併€佷骇鐗╃敓鎴愮殑涓棿浣嶇疆
    public Transform outputAt;    // 浜у嚭鐩爣鐐癸細浜х墿鏈€缁堢Щ鍔ㄥ埌鐨勭洰鏍囦綅缃?

    [Header("Section 4")]
    [Tooltip("Info 2")]
    public float inputToCenterSpeed = 5f; 
    [Tooltip("Info 3")]
    public float centerToOutputSpeed = 5f; 

    // ==========================================
    // 鐘舵€佹爣璁帮紙闃叉閲嶅鎵ц銆佹帶鍒舵祦绋嬶級
    // ==========================================
    private bool _isGathering = false; // 鏄惁姝ｅ湪鏀堕泦鍘熸枡
    private bool _isSpawning = false;  // 鏄惁姝ｅ湪鐢熸垚浜х墿
    private int _pendingOutputs = 0;   // 寰呯敓鎴愮殑浜х墿鏁伴噺锛堝師鏂欏凡娑堣€楋紝绛夊緟鐢熸垚锛?

    // 宸插惛鍏ョ殑鍘熸枡璁℃暟
    private int _currentBag1Sucked = 0;
    private int _currentBag2Sucked = 0;
        private void OnEnable()
        {
        // 鍒濆鍖栨墍鏈夌姸鎬?
        _isGathering = false;
        _isSpawning = false;
        _pendingOutputs = 0;
    }

    private void Update()
    {
        // 鍏抽敭鍙傛暟缂哄け锛岀洿鎺ヨ烦杩?
        if (bag1 == null || bag2 == null || outputStock == null || inputAt == null || centerAt == null || outputAt == null || string.IsNullOrEmpty(outputStaffName))
        {
            Debug.LogWarning($"[{GetType().Name}] Missing required configuration.");
            Next();
            return;
        }

        // --------------------------------------------------------
        // 閫昏緫1锛氬鐞嗕骇鐗╃敓鎴愰槦鍒?
        // 鏈夊緟鐢熸垚浜х墿涓旀湭鍦ㄧ敓鎴?鈫?寮€濮嬬敓鎴?
        // --------------------------------------------------------
        if (_pendingOutputs > 0 && !_isSpawning)
        {
            _isSpawning = true;
            _pendingOutputs--;
            SpawnAndMoveOutput();
        }

        // --------------------------------------------------------
        // 閫昏緫2锛氭鏌ユ槸鍚﹀彲浠ュ紑濮嬫敹闆嗗師鏂?
        // 鏉′欢锛氬師鏂欒冻澶?+ 浜у嚭搴撳瓨鏈弧 + 涓嶅湪鏀堕泦杩囩▼涓?
        // --------------------------------------------------------
        if (!_isGathering)
        {
            // 璁＄畻浜у嚭搴撳瓨棰勮鏁伴噺锛堝綋鍓?鐢熸垚涓?寰呯敓鎴愶級
            int projectedOutputCount = outputStock.Count() + (_isSpawning ? 1 : 0) + _pendingOutputs;
            
            if (bag1.CompleteCount() >= bag1NeededNum && 
                bag2.CompleteCount() >= bag2NeededNum && 
                projectedOutputCount < outputStock.capacity)
            {
                _isGathering = true;
                StartGatheringBatch();
            }
        }
    }

    private void StartGatheringBatch()
    {
        _currentBag1Sucked = 0;
        _currentBag2Sucked = 0;

        // 鏃犲師鏂欓渶姹傦紝鐩存帴鐢熸垚浜х墿
        if (bag1NeededNum == 0 && bag2NeededNum == 0)
        {
            _pendingOutputs++;
            _isGathering = false;
            return;
        }

        // 寮€濮嬮€愪釜鍚稿叆鍘熸枡
        SuckNextItem();
    }

    // 渚濇鍚稿叆鍘熸枡锛氬厛鍚竍ag1锛屽啀鍚竍ag2锛屽叏閮ㄥ惛瀹屽悗鏍囪鍙敓鎴愪骇鐗?
    private void SuckNextItem()
    {
        if (_currentBag1Sucked < bag1NeededNum)
        {
            _currentBag1Sucked++;
            AnimateSingleMaterial(bag1, SuckNextItem);
        }
        else if (_currentBag2Sucked < bag2NeededNum)
        {
            _currentBag2Sucked++;
            AnimateSingleMaterial(bag2, SuckNextItem);
        }
        else
        {
            // ==========================================
            // 鎵€鏈夊師鏂欏凡鏀堕泦瀹屾垚 鈫?澧炲姞寰呯敓鎴愪骇鐗╄鏁?
            // ==========================================
            _pendingOutputs++;
            
            // 閲嶇疆鏀堕泦鐘舵€侊紝鍏佽涓嬩竴杞悎鎴?
            _isGathering = false; 
        }
    }

    // 鎵ц鍗曚釜鍘熸枡鐨勫姩鐢伙細搴撳瓨 鈫?鍚稿叆鐐?鈫?涓績鐐?鈫?閿€姣?
    private void AnimateSingleMaterial(Stock targetBag, Action onComplete)
    {
        targetBag.TakeAwayWorld(inputAt.position, false, (item) =>
        {
            if (item == null)
            {
                onComplete?.Invoke(); // 鏃犵墿鍝侊紝鐩存帴鍥炶皟
                return;
            }

            item.DOMove(centerAt.position, inputToCenterSpeed).SetSpeedBased(true).SetEase(Ease.Linear).OnComplete(() =>
            {
                // 鐗╁搧鍒拌揪涓績鐐瑰悗鍥炴敹鍒板璞℃睜
                Root.Instance.resourceCenter.Release(item.gameObject);
                
                // 鍥炶皟锛氱户缁惛鍏ヤ笅涓€涓墿鍝?
                onComplete?.Invoke();
            });
        });
    }

    // 鐢熸垚浜х墿骞剁Щ鍔ㄥ埌浜у嚭鐐?
    private void SpawnAndMoveOutput()
    {
        GameObject newObj = Root.Instance.resourceCenter.SpawnPrefab(
            outputStaffName, 
            centerAt.position, 
            Quaternion.identity
        );
        
        if (newObj == null)
        {
            Debug.LogError($"[{GetType().Name}] Failed to spawn output prefab: {outputStaffName}.");
            _isSpawning = false; // 閲嶇疆鐘舵€?
            return;
        }

        Transform newStaff = newObj.transform;

        newStaff.DOMove(outputAt.position, centerToOutputSpeed).SetSpeedBased(true).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            outputStock.StockIn(newStaff, true);
            
            // 鐢熸垚瀹屾垚锛岄噸缃姸鎬?
            _isSpawning = false; 
        });
    }
}





