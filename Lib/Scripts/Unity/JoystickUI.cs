﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SP {
    public enum DisplayModel
    {
        Always,
        Touch,
    }
    public class JoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,IMMVarTarget {
        
        [SerializeField] protected RectTransform background = null;
        [SerializeField] private RectTransform handleRt = null;
        
        [SerializeField] public DisplayModel _displayModel = DisplayModel.Always;
        public bool touchChangePos = false;
        
        public float Horizontal { get { return (snapX) ? SnapFloat(input.x, AxisOptions.Horizontal) : input.x; } }
        public float Vertical { get { return (snapY) ? SnapFloat(input.y, AxisOptions.Vertical) : input.y; } }

        // public Vector2 Direction { get { return new Vector2(Horizontal, Vertical); } }
        [Output]
        public Vector2 direction=new Vector2();
        public float HandleRange {
            get { return handleRange; }
            set { handleRange = Mathf.Abs(value); }
        }

        public float DeadZone {
            get { return deadZone; }
            set { deadZone = Mathf.Abs(value); }
        }

        public AxisOptions AxisOptions { get { return AxisOptions; } set { axisOptions = value; } }
        public bool SnapX { get { return snapX; } set { snapX = value; } }
        public bool SnapY { get { return snapY; } set { snapY = value; } }

        private float handleRange = 1;
        
        private float deadZone = 0;
        private AxisOptions axisOptions = AxisOptions.Both;
        private bool snapX = false;
        private bool snapY = false;

        private RectTransform baseRect = null;

        private Canvas canvas;
        private Camera cam;

        private Vector2 input = Vector2.zero;
        
        
        public UnityEvent onClickDown=new UnityEvent();
        public UnityEvent onClickUp=new UnityEvent();

        private void Awake()
        {
            gg.Ins.CreateEventSystemIfNotExists();
        }

        private void Update()
        {
            direction.x = Horizontal;
            direction.y = Vertical;
        }

        public void Start() {
            HandleRange = handleRange;
            DeadZone = deadZone;
            baseRect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gg.Ins.GetOrCreateCanvas();
            }

            if (background != null)
            {
                Vector2 center = new Vector2(0.5f, 0.5f);
                background.pivot = center;
                handleRt.anchorMin = center;
                handleRt.anchorMax = center;
                handleRt.pivot = center;
                handleRt.anchoredPosition = Vector2.zero;
                
                background.gameObject.SetActive(_displayModel==DisplayModel.Always);
            }
            
            
        }

        public virtual void OnPointerDown(PointerEventData eventData) {
            background.gameObject.SetActive(true);
            if(touchChangePos)
                background.transform.position = new Vector2(eventData.position.x, eventData.position.y);
            
            onClickDown.Invoke();
        }


        public virtual void OnPointerUp(PointerEventData eventData) {
            if (_displayModel == DisplayModel.Touch)
            {
                background.gameObject.SetActive(false);
                
            }
            input = Vector2.zero;
            handleRt.anchoredPosition = Vector2.zero;
            onClickUp.Invoke(); 
        }
        // public void OnPointerUp(PointerEventData eventData)
        // {
        //     background.gameObject.SetActive(false);
        //     base.OnPointerUp(eventData);
        //
        // }

        public void OnDrag(PointerEventData eventData) {
            cam = null;
            // if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            //     cam = canvas.worldCamera;
            
            Vector2 position = RectTransformUtility.WorldToScreenPoint(cam, background.position);
            Vector2 radius = background.sizeDelta / 2;
            input = (eventData.position - position) / (radius * canvas.scaleFactor);
            FormatInput();
            HandleInput(input.magnitude, input.normalized, radius, cam);
            handleRt.anchoredPosition = input * radius * handleRange;
        }

        protected virtual void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam) {
            if (magnitude > deadZone) {
                if (magnitude > 1)
                    input = normalised;
            }
            else
                input = Vector2.zero;
        }

        private void FormatInput() {
            if (axisOptions == AxisOptions.Horizontal)
                input = new Vector2(input.x, 0f);
            else if (axisOptions == AxisOptions.Vertical)
                input = new Vector2(0f, input.y);
        }

        private float SnapFloat(float value, AxisOptions snapAxis) {
            if (value == 0)
                return value;

            if (axisOptions == AxisOptions.Both) {
                float angle = Vector2.Angle(input, Vector2.up);
                if (snapAxis == AxisOptions.Horizontal) {
                    if (angle < 22.5f || angle > 157.5f)
                        return 0;
                    else
                        return (value > 0) ? 1 : -1;
                }
                else if (snapAxis == AxisOptions.Vertical) {
                    if (angle > 67.5f && angle < 112.5f)
                        return 0;
                    else
                        return (value > 0) ? 1 : -1;
                }
                return value;
            } else {
                if (value > 0)
                    return 1;
                if (value < 0)
                    return -1;
            }
            return 0;
        }


        protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition) {
            Vector2 localPoint = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, screenPosition, cam, out localPoint)) {
                Vector2 pivotOffset = baseRect.pivot * baseRect.sizeDelta;
                return localPoint - (background.anchorMax * baseRect.sizeDelta) + pivotOffset;
            }
            return Vector2.zero;
        }
    }
    public enum AxisOptions { Both, Horizontal, Vertical }
}
