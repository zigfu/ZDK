using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

class NewDataEventArgs : EventArgs
{
    public string JsonData { get; private set; }
    public NewDataEventArgs(string jsonData) { JsonData = jsonData; }
}

class PluginSettingsEventArgs : EventArgs
{
    public int ImageMapX;
    public int ImageMapY;
    public int DepthMapX;
    public int DepthMapY;
    public int LabelMapX;
    public int LabelMapY;
}

class WebplayerReceiver : MonoBehaviour
{
    static bool loaded = false;
    static bool earlyUpdateLabelMap = false;
    static bool earlyUpdateDepth = false;
    static bool earlyUpdateImage = false;
    const string jsToInject = @"
	if (typeof webplayerInitZigPlugin == 'undefined') {
		function addHandler(target, eventName, handlerName) {
			if ( target.attachEvent ) target.attachEvent('on' + eventName, handlerName);
			else if ( target.addEventListener ) target.addEventListener(eventName, handlerName, false);
			else target['on' + eventName] = handlerName;
		}
		WebplayerIds = [];
		function webplayerOnNewData(data) {
			var plugin = GetZigObject();
			for (webplayerId in WebplayerIds) {
				var unity = unityObject.getObjectById(webplayerId);
				if (null == unity) continue;
				unity.SendMessage(WebplayerIds[webplayerId], 'NewData', data);
                if (typeof streamsRequested == 'undefined') continue;
                if (streamsRequested.depth) {
                    unity.SendMessage(WebplayerIds[webplayerId], 'NewDepth', plugin.depthMap);
                }
                if (streamsRequested.image) { 
                    unity.SendMessage(WebplayerIds[webplayerId], 'NewImage', plugin.imageMap);
                }
			}
		}
        function sendLoaded(playerId, objectName) {
	        var unity = unityObject.getObjectById(playerId);
            if (null == unity) return;
            console.log('sending DoneLoading to dom element ' + playerId + ', obj: ' + objectName);
            unity.SendMessage(objectName, 'DoneLoading', '');
        }
		
		function webplayerZigPluginLoaded(plugin)
		{
			if (undefined === plugin) {
				plugin = document.getElementById('zigPluginObject');
			}
			addHandler(plugin, 'NewFrame', webplayerOnNewData);
            //TODO: finish support on the ZigInput side
			//for (webplayerId in WebplayerIds) {
			//	var unity = unityObject.getObjectById(webplayerId);
			//	if (null == unity) continue;
            //    unity.SendMessage(WebplayerIds[webplayerId], 'PluginSettings', JSON.stringify(
            //                        {imageMapResolution:plugin.imageMapResolution,
            //                         depthMapResolution:plugin.depthMapResolution,}));
            //}
		}

		CachedZigObject = null;
		function GetZigObject()
		{
			if (typeof CachedZigObject == 'undefined') {
				CachedZigObject = null;
			}

			if (null == CachedZigObject) {
				var objs = document.getElementsByTagName('object');
				for (var i=0; i<objs.length; i++) {
					if (objs[i].requestStreams !== undefined) {
						CachedZigObject = objs[i];
						break;
					}
				}
			}
			return CachedZigObject;
		}

		function GetUnreadyZigObject()
		{
			var objs = document.getElementsByTagName('object');
			for (var i=0; i<objs.length; i++) {
				if ((objs[i].type !== undefined)&&(objs[i].type=='application/x-zig')) {
                    console.log('found not-loaded zig object!');
                    console.log(objs[i]);
                    return objs[i];
				}
			}
            return null;
		}

        function setStreams(depth, image) {
            streamsRequested = { depth : depth, image : image };
            var zig = GetZigObject();
            if (zig) zig.requestStreams(depth, image, false);
        }

        function pollPrezig(prezig, playerId, objectName) {
            if (prezig.requestStreams !== undefined) {
                console.log('prezig is now a zig!');
                webplayerInitZigPlugin(playerId,objectName);
            } else {
                console.log('prezig is not yet ready :/');
                setInterval(pollPrezig, 100, prezig, playerId, objectName);
            }
        }

		function webplayerInitZigPlugin(playerId, objectName)
		{
            if (typeof streamsRequested == 'undefined' ) streamsRequested = { depth : false, image : false };
            var preZig = GetUnreadyZigObject();
            if (preZig !== null) {
                console.log('got prezig, polling till valid!');
                sendLoaded(playerId, objectName);
                setInterval(pollPrezig, 100, prezig, playerId, objectName);
                return;
            }
			var zigObject = GetZigObject();
			if (null == zigObject) {
                console.log('Injecting new zig plugin object!');" +
                "var html = '<object id=\"zigPluginObject\" type=\"application/x-zig\" width=\"0\" height=\"0\"><param name=\"onload\" value=\"webplayerZigPluginLoaded\" /></object>';" +
                @"var newDiv = document.createElement('div');
				WebplayerIds = [];
				newDiv.innerHTML = html;
				document.body.appendChild(newDiv);
				zigObject = document.getElementById('zigPluginObject');
                setStreams(streamsRequested.depth, streamsRequested.image);
			} else {
                console.log('using existing zig plugin object!');
				WebplayerIds = [];
				webplayerZigPluginLoaded(zigObject);
			}
			WebplayerIds[playerId] = objectName;
            sendLoaded(playerId, objectName);
		}
	}
	";

    public static void SetStreamsToUpdate(bool updateDepth, bool updateImage, bool updateLabelMap)
    {
        if (!loaded) {
            earlyUpdateImage = updateImage;
            earlyUpdateDepth = updateDepth;
            earlyUpdateLabelMap = updateLabelMap;
        }
        else {
            Application.ExternalEval(string.Format("setStreams({0}, {1}, {2})", updateDepth.ToString().ToLower(), updateImage.ToString().ToLower(), updateLabelMap.ToString().ToLower()));
        }
    }

    const string GameObjectName = "WebplayerReceiver";

    public static WebplayerReceiver Create()
    {
        GameObject go = new GameObject(GameObjectName);
        DontDestroyOnLoad(go);
        WebplayerReceiver result = go.AddComponent<WebplayerReceiver>();
        string toInject = jsToInject + string.Format("webplayerInitZigPlugin('unityPlayer', '{0}')", GameObjectName);
        Application.ExternalEval(toInject);
        return result;
    }
    void DoneLoading()
    {
        loaded = true;
        SetStreamsToUpdate(earlyUpdateDepth, earlyUpdateImage, earlyUpdateLabelMap);
    }
    public event EventHandler<NewDataEventArgs> NewDepthEvent;
    void NewDepth(string param)
    {
        try {
            if (null != NewDepthEvent) {
                NewDepthEvent.Invoke(this, new NewDataEventArgs(param));
            }
        }
        catch (System.Exception ex) {
            // the logger will show exceptions on screen, useful for 
            // webplayer debugging
            WebplayerLogger.Log(ex.ToString());
        }
    }


    public event EventHandler<NewDataEventArgs> NewLabelMapEvent;
    void NewLabelMap(string param)
    {
        try
        {
            if (null != NewLabelMapEvent)
            {
                NewLabelMapEvent.Invoke(this, new NewDataEventArgs(param));
            }
        }
        catch (System.Exception ex)
        {
            // the logger will show exceptions on screen, useful for 
            // webplayer debugging
            WebplayerLogger.Log(ex.ToString());
        }
    }




    public event EventHandler<NewDataEventArgs> NewImageEvent;
    void NewImage(string param)
    {
        try {
            if (null != NewImageEvent) {
                NewImageEvent.Invoke(this, new NewDataEventArgs(param));
            }
        }
        catch (System.Exception ex) {
            // the logger will show exceptions on screen, useful for 
            // webplayer debugging
            WebplayerLogger.Log(ex.ToString());
        }
    }

    public event EventHandler<NewDataEventArgs> NewDataEvent;

    // called from javascript, giving us new data from the plugin
    void NewData(string param)
    {
        try {
            if (null != NewDataEvent) {
                NewDataEvent.Invoke(this, new NewDataEventArgs(param));
            }
        }
        catch (System.Exception ex) {
            // the logger will show exceptions on screen, useful for 
            // webplayer debugging
            WebplayerLogger.Log(ex.ToString());
        }
    }
    //public event EventHandler<PluginSettingsEventArgs> PluginSettingsEvent;
    //void PluginSettings(string param)
    //{
    //    Hashtable settings = (Hashtable)JSON.JsonDecode(param);
    //    Hashtable image = (Hashtable)settings["imageMapSettings"];
    //    Hashtable depth = (Hashtable)settings["depthMapSettings"];
        
    //    try {
    //        if (null != PluginSettingsEvent) {
    //            PluginSettingsEvent.Invoke(this, new PluginSettingsEventArgs() {
    //                ImageMapX = (int)(double)image["width"],
    //                ImageMapY = (int)(double)image["height"],
    //                DepthMapX = (int)(double)depth["width"],
    //                DepthMapY = (int)(double)depth["height"],
    //            });
    //        }
    //    }
    //    catch (System.Exception ex) {
    //        // the logger will show exceptions on screen, useful for 
    //        // webplayer debugging
    //        WebplayerLogger.Log(ex.ToString());
    //    }
    //}
}