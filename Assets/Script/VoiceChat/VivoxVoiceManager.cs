using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
 // Still used for core types like LoginState, ChatCapability
using System.Threading.Tasks;
using Photon.Pun;


public class VoiceChatManager : MonoBehaviour
{
    public static VoiceChatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps the object across scene loads
        }
        else
        {
            Destroy(gameObject); // THIS IS THE CULPRIT ❌
        }
    }

    private async void Start()
    {
        // Start the initialization process when the object is created
        await InitializeVivoxAsync();
    }

    private async Task InitializeVivoxAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await VivoxService.Instance.InitializeAsync();

            LoginOptions options = new LoginOptions
            {
                DisplayName = PhotonNetwork.LocalPlayer.NickName
            };
            await VivoxService.Instance.LoginAsync(options);

            Debug.Log("Vivox Login Successful.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox Setup Failed: {e.Message}");
        }
    }

    /// <summary>
    /// Joins a voice channel.
    /// </summary>
    /// <param name="channelName">The unique name of the channel (e.g., the Photon Room Name).</param>
    public async void JoinChannel(string channelName)
    {
        // FIX: The IsLoggedIn property on IVivoxService replaces checking LoginSession.State
        if (!VivoxService.Instance.IsLoggedIn)
        {
            Debug.LogWarning("Vivox not logged in, cannot join channel.");
            return;
        }

        try
        {
            // Join a standard group voice channel.
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

            Debug.Log($"Joined Vivox channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join Vivox channel {channelName}: {e.Message}");
        }
    }

    /// <summary>
    /// Leaves the voice channel.
    /// </summary>
    /// <param name="channelName">The name of the channel to leave.</param>
    public async void LeaveChannel(string channelName)
    {
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(channelName);
            Debug.Log($"Left Vivox channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to leave Vivox channel {channelName}: {e.Message}");
        }
    }

    /// <summary>
    /// Toggles the local microphone mute state.
    /// </summary>
    /// <param name="isMuted">True to mute, False to unmute.</param>
    public void ToggleMuteSelf(bool isMuted)
    {
        // FIX: Mutex is controlled by MuteInputDevice/UnmuteInputDevice methods directly on IVivoxService
        if (isMuted)
        {
            VivoxService.Instance.MuteInputDevice();
            print("ismuted");
        }
        else
        {
            VivoxService.Instance.UnmuteInputDevice();
            print("isnotmuted");
        }
        Debug.Log($"Local microphone muted: {isMuted}");
    }
}