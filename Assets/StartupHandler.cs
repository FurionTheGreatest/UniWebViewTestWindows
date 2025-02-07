using System;
using UnityEngine;
[RequireComponent(typeof(UniWebView))]
public class StartupHandler : MonoBehaviour
{
    [SerializeField] private string pathToLoad;
    private float _currentYPosition;
    private UniWebView uniView;
    private void Awake()
    {
        uniView = GetComponent<UniWebView>();
    }

    private void Start()
    {
        InitDisplay();
    }

    #region Tricky
    private void GetScrollY(UniWebView uniView, Action<float> callback)
    {
        string script = @"
        (function() {
            // Get scroll position with maximum precision
            var scrollY = window.pageYOffset || 
                         document.documentElement.scrollTop || 
                         document.body.scrollTop || 0;
            // Convert to string with fixed format to ensure reliable parsing
            return scrollY.toString();
        })();
    ";

        uniView.EvaluateJavaScript(script, (payload) => {
            if (payload.resultCode.Equals("0") && !string.IsNullOrEmpty(payload.data))
            {
                try 
                {
                    float scrollY = float.Parse(payload.data, 
                        System.Globalization.CultureInfo.InvariantCulture);
                    callback(scrollY);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Scroll position parsing failed. Raw value: {payload.data}, Error: {e.Message}");
                    string cleanValue = payload.data.Trim().Replace(",", ".");
                    if (float.TryParse(cleanValue, 
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, 
                            out float fallbackValue))
                    {
                        callback(fallbackValue);
                    }
                    else
                    {
                        Debug.LogError("Could not parse scroll position even after cleanup");
                        callback(0f);
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to get scroll position: " + 
                               (payload.data ?? "No data received"));
                callback(0f);
            }
        });
    }
    

    #endregion
    private void OnEnable()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        
    }
    private void InitDisplay()
    {
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        uniView.Frame = new Rect(0, 0, Screen.width, Screen.height);

        UniWebView.SetAllowJavaScriptOpenWindow(true);
        UniWebView.SetJavaScriptEnabled(true);
        UniWebView.SetWebContentsDebuggingEnabled(true);
        uniView.SetAllowFileAccess(true);
        uniView.SetUseWideViewPort(true);
        uniView.SetCacheMode(UniWebViewCacheMode.NoCache);
        uniView.SetSupportMultipleWindows(true, true);
        uniView.SetZoomEnabled(true);
        uniView.SetAcceptThirdPartyCookies(true);
        uniView.SetBouncesEnabled(true);
        uniView.OnOrientationChanged += UniViewOnOnOrientationChanged;
        uniView.OnPageStarted += OnLoadStart;
        uniView.OnLoadingErrorReceived += OnLoadError;
        uniView.OnShouldClose += CloseApplication; 
        //uniView.OnMultipleWindowOpened += OnMultipleWindowOpened;
        //uniView.OnMultipleWindowClosed += OnMultipleWindowClosed;
        uniView.Load(pathToLoad);
        uniView.Show();
        Screen.fullScreen = false;
    }
    private void OnMultipleWindowClosed(UniWebView webview, string multiplewindowid)
    {
        webview.ScrollTo(0,(int)_currentYPosition,false);
        webview.UpdateFrame();
    }
    private void OnMultipleWindowOpened(UniWebView webview, string multiplewindowid)
    {
        Debug.Log("Multiple window opened");
        GetScrollY(webview,pos => _currentYPosition = pos);
        webview.ScrollTo(0,0,false);
        webview.UpdateFrame();
        
    }
    private void UniViewOnOnOrientationChanged(UniWebView webview, ScreenOrientation orientation)
    {
        webview.Frame = new Rect(0, 0, Screen.width, Screen.height);
        webview.UpdateFrame();
    }

    private bool CloseApplication(UniWebView engine)
    {
        Application.Quit();
        return false;
    }


    private void OnLoadStart(UniWebView engine, string url)
    {
        if (!url.Contains("http://") && 
            !url.Contains("https://") && 
            !url.Contains("about"))
        {
            Application.OpenURL(url);
            engine.Stop();
            engine.GoBack();
        }
    }
    

    private void OnLoadError(UniWebView engine, int code, string msg,
        UniWebViewNativeResultPayload nativePayload)
    {
    }

}
