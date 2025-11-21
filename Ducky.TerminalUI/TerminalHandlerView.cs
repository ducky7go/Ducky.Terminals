using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ducky.TerminalUI;

/// <summary>
/// 左下角触发区域，鼠标悬停时从底部滑出1/4圆圈按钮
/// </summary>
public class TerminalHandlerView : MonoBehaviour
{
    [Header("触发区域设置")] [SerializeField] private RectTransform? triggerArea;

    [Header("圆圈按钮")] [SerializeField] private GameObject? circleButton;
    [SerializeField] private RectTransform? circleButtonRect;
    [SerializeField] private Button? button;

    [Header("动画设置")] [SerializeField] private float slideSpeed = 5f;
    [SerializeField] private float hiddenYPosition = -100f; // 隐藏位置（屏幕外）
    [SerializeField] private float visibleYPosition = 0f; // 可见位置（左下角完全可见）

    private bool isMouseOver = false;
    private TerminalMainView? _mainView;

    private void Awake()
    {
        Log.Info("[TerminalHandlerView] Awake() called");

        // 确保初始状态是隐藏的
        if (circleButtonRect != null)
        {
            var pos = circleButtonRect.anchoredPosition;
            pos.y = hiddenYPosition;
            circleButtonRect.anchoredPosition = pos;
            Log.Info($"[TerminalHandlerView] Circle button initial position: {pos}");
        }
    }

    private void Start()
    {
        Log.Info("[TerminalHandlerView] Start() called");

        // 在 Start() 中绑定按钮点击事件（此时所有字段都已赋值）
        if (button != null)
        {
            Log.Info("[TerminalHandlerView] Adding click listener to button");
            button.onClick.AddListener(OnCircleButtonClicked);
        }
        else
        {
            Log.Error("[TerminalHandlerView] Button is still null in Start!");
        }
    }

    internal void Initialize(TerminalMainView mainView)
    {
        Log.Info($"[TerminalHandlerView] Initialize called with mainView={(mainView != null ? "valid" : "NULL")}");
        _mainView = mainView;
    }

    private void Update()
    {
        // 检查鼠标是否在触发区域内
        if (triggerArea != null && RectTransformUtility.RectangleContainsScreenPoint(triggerArea, Input.mousePosition, null))
        {
            // 如果主面板已经展示，不响应触发区域
            if (_mainView != null && _mainView.IsVisible)
            {
                if (isMouseOver)
                {
                    Log.Info("[TerminalHandlerView] Main panel is visible, hiding trigger");
                    isMouseOver = false;
                    StopAllCoroutines();
                    StartCoroutine(SlideCircle(hiddenYPosition));
                }
                return;
            }

            if (!isMouseOver)
            {
                Log.Info("[TerminalHandlerView] Mouse entered trigger area");
                isMouseOver = true;
                StopAllCoroutines();
                StartCoroutine(SlideCircle(visibleYPosition));
            }
        }
        else
        {
            if (isMouseOver)
            {
                Log.Info("[TerminalHandlerView] Mouse exited trigger area");
                isMouseOver = false;
                StopAllCoroutines();
                StartCoroutine(SlideCircle(hiddenYPosition));
            }
        }
    }

    /// <summary>
    /// 设置触发区域的激活状态
    /// </summary>
    public void SetActive(bool active)
    {
        if (triggerArea != null)
        {
            triggerArea.gameObject.SetActive(active);
            Log.Info($"[TerminalHandlerView] Trigger area set to {(active ? "active" : "inactive")}");

            // 如果禁用，确保圆圈按钮也隐藏
            if (!active && circleButtonRect != null)
            {
                StopAllCoroutines();
                var pos = circleButtonRect.anchoredPosition;
                pos.y = hiddenYPosition;
                circleButtonRect.anchoredPosition = pos;
            }
        }
    }

    private IEnumerator SlideCircle(float targetY)
    {
        if (circleButtonRect == null) yield break;

        while (Mathf.Abs(circleButtonRect.anchoredPosition.y - targetY) > 0.1f)
        {
            var pos = circleButtonRect.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * slideSpeed);
            circleButtonRect.anchoredPosition = pos;
            yield return null;
        }

        // 确保最终位置精确
        var finalPos = circleButtonRect.anchoredPosition;
        finalPos.y = targetY;
        circleButtonRect.anchoredPosition = finalPos;
    }

    private void OnCircleButtonClicked()
    {
        Log.Info("[TerminalHandlerView] Circle button clicked!");
        if (_mainView != null)
        {
            Log.Info("[TerminalHandlerView] Calling mainView.Show()");
            _mainView.Show();
        }
        else
        {
            Log.Error("[TerminalHandlerView] mainView is null!");
        }
    }

    /// <summary>
    /// 创建1/4圆形 Sprite（左下角）
    /// </summary>
    private static Sprite CreateQuarterCircleSprite(int resolution)
    {
        var center = 0; // 圆心在左下角
        float radius = resolution - 1;

        var texture = new Texture2D(resolution, resolution);
        var pixels = new Color[resolution * resolution];

        for (var y = 0; y < resolution; y++)
        {
            for (var x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);

                // 只保留右上象限的圆弧（从左下角开始的1/4圆）
                if (distance <= radius - 1 && x >= center && y >= center)
                {
                    pixels[y * resolution + x] = Color.white;
                }
                else if (distance <= radius && x >= center && y >= center)
                {
                    var alpha = radius - distance;
                    pixels[y * resolution + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // 将pivot设置在左下角，这样1/4圆会正确显示在左下角
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0, 0));
    }

    /// <summary>
    /// 创建UI结构的静态工厂方法
    /// </summary>
    internal static TerminalHandlerView Create(Canvas canvas, TerminalMainView mainView)
    {
        Log.Info("[TerminalHandlerView] Creating UI structure...");

        // 创建触发区域容器
        var triggerObj = new GameObject("TerminalTriggerArea");
        triggerObj.transform.SetParent(canvas.transform, false);

        var triggerRect = triggerObj.AddComponent<RectTransform>();
        triggerRect.anchorMin = new Vector2(0, 0); // 左下角
        triggerRect.anchorMax = new Vector2(0, 0);
        triggerRect.pivot = new Vector2(0, 0);
        triggerRect.anchoredPosition = Vector2.zero;
        triggerRect.sizeDelta = new Vector2(150, 150); // 触发区域大小

        // 添加Image组件（仅用于显示，不接收射线）
        var triggerImage = triggerObj.AddComponent<Image>();
        triggerImage.color = new Color(0, 0, 0, 0); // 完全透明
        triggerImage.raycastTarget = false; // 不接收射线，允许点击穿透

        Log.Info("[TerminalHandlerView] Trigger area created");

        // 创建1/4圆圈按钮
        var circleObj = new GameObject("QuarterCircleButton");
        circleObj.transform.SetParent(triggerObj.transform, false);

        var circleRect = circleObj.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(0, 0); // 左下角锚点
        circleRect.anchorMax = new Vector2(0, 0);
        circleRect.pivot = new Vector2(0, 0); // Pivot也设置在左下角
        circleRect.anchoredPosition = new Vector2(0, -100); // 初始隐藏位置
        circleRect.sizeDelta = new Vector2(60, 60); // 1/4圆圈大小

        // 添加按钮组件
        var circleImage = circleObj.AddComponent<Image>();
        circleImage.color = new Color(0.2f, 0.6f, 1f, 0.8f); // 蓝色半透明
        circleImage.raycastTarget = true; // 确保按钮可以接收点击

        // 创建1/4圆形 Sprite
        circleImage.sprite = CreateQuarterCircleSprite(60);
        circleImage.type = Image.Type.Simple;
        circleImage.preserveAspect = true;

        var button = circleObj.AddComponent<Button>();

        Log.Info("[TerminalHandlerView] Circle button created");

        // 添加主脚本
        var handler = triggerObj.AddComponent<TerminalHandlerView>();
        handler.triggerArea = triggerRect;
        handler.circleButton = circleObj;
        handler.circleButtonRect = circleRect;
        handler.button = button;
        handler.Initialize(mainView);

        Log.Info("[TerminalHandlerView] UI structure created successfully");
        return handler;
    }
}
