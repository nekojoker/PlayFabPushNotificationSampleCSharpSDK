using System;
using System.Collections;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif
using PlayFab.Json;

public class PlayFabController : MonoBehaviour
{
    private string pushToken;
    private string lastMsg;
    private string deviceToken;

    // OnGUI should be deleted/replaced with your own gui - This is only provided for debugging
    public void OnGUI()
    {
        GUI.Label(new Rect(0, 0, Screen.width, 200), pushToken);
        GUI.Label(new Rect(0, 200, Screen.width, Screen.height - 200), lastMsg);
    }
    async void Start()
    {

#if UNITY_ANDROID
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
#endif

        PlayFabSettings.staticSettings.TitleId = "A80F6";

#if UNITY_IOS
        StartCoroutine(RequestAuthorization());
#endif

        var request = new LoginWithCustomIDRequest
        {
            CustomId = "GettingStartedGuide",
            CreateAccount = true,
        };
        var result = await PlayFabClientAPI.LoginWithCustomIDAsync(request);

        if (result.Error != null)
        {
            Debug.Log(result.Error.GenerateErrorReport());
        }
        else
        {
            Debug.Log("Login Success!!");
#if UNITY_ANDROID
            RegisterForPush();
#elif UNITY_IOS
            // must be called before trying to obtain the push token
            // an asynchronous call with no callback into native iOS code that takes a moment or two before
            // the token is available. (so spin and wait, or call this one early on)
            // this will always return null if your app is not signed
            RegisterForIOSPushNotification();
#endif
        }
    }

#if UNITY_ANDROID
    private async void RegisterForPush()
    {
        if (string.IsNullOrEmpty(pushToken))
            return;

        var request = new AndroidDevicePushNotificationRegistrationRequest
        {
            DeviceToken = pushToken,
            SendPushNotificationConfirmation = true,
            ConfirmationMessage = "Push notifications registered successfully"
        };
        var result = await PlayFabClientAPI.AndroidDevicePushNotificationRegistrationAsync(request);

        string message = result.Error is null
            ? "PlayFab: Push Registration Successful"
            : result.Error.GenerateErrorReport();

        Debug.Log(message);
    }
    private void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        Debug.Log("PlayFab: Received Registration Token: " + token.Token);
        pushToken = token.Token;
        RegisterForPush();
    }

    private void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
    {
        Debug.Log("PlayFab: Received a new message from: " + e.Message.From);
        lastMsg = "";
        if (e.Message.Data != null)
        {
            lastMsg += "DATA: " + PlayFabSimpleJson.SerializeObject(e.Message.Data) + "\n";
            Debug.Log("PlayFab: Received a message with data:");
            foreach (var pair in e.Message.Data)
                Debug.Log("PlayFab data element: " + pair.Key + "," + pair.Value);
        }
        if (e.Message.Notification != null)
        {
            Debug.Log("PlayFab: Received a notification:");
            lastMsg += "TITLE: " + e.Message.Notification.Title + "\n";
            lastMsg += "BODY: " + e.Message.Notification.Body + "\n";
        }
    }
#endif

#if UNITY_IOS
    IEnumerator RequestAuthorization()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            };

            string res = "\n RequestAuthorization:";
            res += "\n finished: " + req.IsFinished;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;
            Debug.Log(res);

            deviceToken = req.DeviceToken;
        }
    }

    public async void RegisterForIOSPushNotification()
    {
        if (string.IsNullOrEmpty(deviceToken))
        {
            var request = new RegisterForIOSPushNotificationRequest
            {
                DeviceToken = deviceToken
            };
            var result = await PlayFabClientAPI.RegisterForIOSPushNotificationAsync(request);

            string message = result.Error is null
                ? "Push Registration Successful"
                : result.Error.GenerateErrorReport();

            Debug.Log(message);
        }
        else
        {
            Debug.Log("Push Token was null!");
        }
    }
#endif


}
