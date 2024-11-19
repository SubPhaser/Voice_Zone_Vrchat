using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

public class VoiceZone : UdonSharpBehaviour
{
    [SerializeField] private Collider[] zoneColliders;
    [SerializeField] private float insideVoiceGain = 15f;
    [SerializeField] private float insideVoiceDistanceFar = 25f;
    [SerializeField] private float outsideVoiceGain = 0f;
    [SerializeField] private float outsideVoiceDistanceFar = 0f;
    [SerializeField] private float updateInterval = 1f;

    private VRCPlayerApi localPlayer;
    private VRCPlayerApi[] players;
    private int[] playerZones;
    private float updateTimer = 0f;

    private void Start()
    {
        if (zoneColliders == null || zoneColliders.Length == 0)
        {
            Debug.LogError("[VoiceZone] Voice Zone Colliders are not assigned!");
            return;
        }

        localPlayer = Networking.LocalPlayer;
        InitializePlayerTracking();
        UpdateAllPlayersAudio();
    }

    private void InitializePlayerTracking()
    {
        players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        playerZones = new int[players.Length];

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsValid())
            {
                playerZones[i] = GetPlayerZone(players[i]);
            }
            else
            {
                playerZones[i] = -1;
            }
        }
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            CheckAndUpdateAllPlayerZones();
        }
    }

    private void CheckAndUpdateAllPlayerZones()
    {
        bool zonesChanged = false;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsValid())
            {
                int newZone = GetPlayerZone(players[i]);
                if (newZone != playerZones[i])
                {
                    Debug.Log($"[VoiceZone] Player {players[i].displayName} changed zone from {playerZones[i]} to {newZone}");
                    playerZones[i] = newZone;
                    zonesChanged = true;
                }
            }
        }

        if (zonesChanged)
        {
            UpdateAllPlayersAudio();
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        InitializePlayerTracking();
        UpdateAllPlayersAudio();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        InitializePlayerTracking();
        UpdateAllPlayersAudio();
    }

    private void UpdateAllPlayersAudio()
    {
        if (localPlayer == null || !localPlayer.IsValid()) return;

        int localPlayerZone = GetPlayerZone(localPlayer);
        Debug.Log($"[VoiceZone] Updating voice parameters. Local player in zone: {localPlayerZone}");

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsValid() && players[i] != localPlayer)
            {
                bool canHear = (localPlayerZone == playerZones[i]) && (localPlayerZone != -1);
                SetPlayerVoiceParameters(players[i], canHear);
                Debug.Log($"[VoiceZone] Player {players[i].displayName} is in zone {playerZones[i]}. Can hear: {canHear}");
            }
        }
    }

    private int GetPlayerZone(VRCPlayerApi player)
    {
        if (player == null || !player.IsValid()) return -1;

        Vector3 playerPosition = player.GetPosition();
        for (int i = 0; i < zoneColliders.Length; i++)
        {
            if (zoneColliders[i] != null && zoneColliders[i].bounds.Contains(playerPosition))
            {
                return i;
            }
        }
        return -1;
    }

    private void SetPlayerVoiceParameters(VRCPlayerApi targetPlayer, bool canHear)
    {
        if (targetPlayer == null || !targetPlayer.IsValid()) return;

        // Voice settings only
        targetPlayer.SetVoiceGain(canHear ? insideVoiceGain : outsideVoiceGain);
        targetPlayer.SetVoiceDistanceFar(canHear ? insideVoiceDistanceFar : outsideVoiceDistanceFar);
        targetPlayer.SetVoiceDistanceNear(0f);
        targetPlayer.SetVoiceVolumetricRadius(0f);
        targetPlayer.SetVoiceLowpass(!canHear);

        // Explicitly not touching avatar audio settings
        // Avatar audio will use default VRChat settings or settings defined by the avatar creator

        Debug.Log($"[VoiceZone] Set voice parameters for player {targetPlayer.displayName}. Can hear: {canHear}");
    }
}