using AdminToys;
using LabApi.Features.Wrappers;
using MEC;
using MingleGame.Core.Components;
using MingleGame.Tools;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using System.Collections.Generic;
using UnityEngine;

namespace MingleGame.Core;

internal class MingleGameLocation
{
    #region Fields

    private readonly SchematicObject _location;
    private readonly Transform _main;

    private readonly List<Room> _rooms = [];

    private readonly PrimitiveObjectToy[] _platformColliders;

    #region Lights
    private readonly LightSourceToy[] _allLights;
    private readonly LightSourceToy[] _topLights;
    private readonly LightSourceToy[] _roomLights;
    private readonly LightSourceToy[] _ambientLights;

    private readonly float _defaultTopLightsIntensity;
    private readonly float _defaultRoomLightsIntensity;
    private readonly float _defaultAmbientLightsIntensity;
    #endregion

    private readonly Color _defaultLightsColor;
    private readonly Color[] _dangerPartColors = [Color.cyan, Color.green, Color.red, Color.magenta, Color.blue, Color.yellow];

    private readonly AudioPlayer _audioPlayer;

    private readonly List<Pony> _ponies = [];

    private readonly Transform _mainPlatform;

    internal readonly List<Room> safeRooms = [];

    public readonly IReadOnlyCollection<Room> rooms;

    #endregion

    #region Methods

    internal MingleGameLocation(Vector3 spawnPosition, Quaternion rotation)
    {
        _location = ObjectSpawner.SpawnSchematic("MingleGame", spawnPosition, rotation);

        _main = _location.transform.Find("Main");
        _mainPlatform = _location.transform.Find("Center/MainPlatform");
        _platformColliders = _location.transform.Find("Center/PlatformColliders").GetComponentsInChildren<PrimitiveObjectToy>();

        #region Lights
        _allLights = _main.GetComponentsInChildren<LightSourceToy>();
        _topLights = _main.Find("Top").GetComponentsInChildren<LightSourceToy>();
        _ambientLights = _main.Find("AmbientLights").GetComponentsInChildren<LightSourceToy>();
        _roomLights = _main.Find("Middle/Rooms").GetComponentsInChildren<LightSourceToy>();

        _defaultLightsColor = _ambientLights[0].NetworkLightColor;
        _defaultTopLightsIntensity = _topLights[0].NetworkLightIntensity;
        _defaultRoomLightsIntensity = _roomLights[0].NetworkLightIntensity;
        _defaultAmbientLightsIntensity = _ambientLights[0].NetworkLightIntensity;
        #endregion

        #region Rooms
        var roomsParent = _main.Find("Middle/Rooms");

        foreach (Transform room in roomsParent.transform)
        {
            if (room.name.StartsWith("Room"))
                _rooms.Add(room.gameObject.AddComponent<Room>());
        }

        rooms = _rooms.ToArray();
        #endregion

        #region Ponies
        var ponies = _location.transform.Find("Center/Decor/Ponies");
        foreach (Transform pony in ponies)
        {
            var component = pony.Find("Pony").gameObject.AddComponent<Pony>();
            component.StartPosition = pony.Find("StartPosition").position;
            component.EndPosition = pony.Find("EndPosition").position;
            component.enabled = false;
            _ponies.Add(component);
        }
        #endregion

        _audioPlayer = AudioPlayer.CreateOrGet("MingleGameAudioPlayer", onIntialCreation: obj =>
        {
            obj.AddSpeaker("MingleGameSpeaker", isSpatial: false, maxDistance: 50f);
            obj.SetSpeakerPosition("MingleGameSpeaker", _location.transform.position);
            obj.transform.parent = _location.transform;
        });
    }

    public Vector3 GetRandomSpawnPosition()
    {
        var halfX = _mainPlatform.localScale.x / 2f;
        var halfZ = _mainPlatform.localScale.z / 2f;
        var offset = 4f;

        return _mainPlatform.position 
                + Vector3.up 
                + new Vector3(
                    Random.Range(-halfX + offset, halfX - offset), 
                    0,
                    Random.Range(-halfZ + offset, halfZ - offset)
                );
    }

    internal void Destroy()
        => _location.Destroy();

    internal void OnStart()
        => _allLights.ForEach(l => l.NetworkLightIntensity = 0f);

    internal void OnStartingCalmPart(float duration)
    {
        _audioPlayer.RemoveAllClips();
        _audioPlayer.AddClip(AudioClipNames.CalmPart);

        _platformColliders.ForEach(o => o.NetworkPrimitiveFlags = PrimitiveFlags.Collidable);
        _ponies.ForEach(p => p.enabled = true);

        foreach (var room in rooms)
        {
            room.CloseDoor();
            room.IsDoorLocked = true;
        }

        foreach (var ragdoll in Ragdoll.List)
            ragdoll.Destroy();

        foreach (var pickup in Pickup.List)
            pickup.Destroy();

        var platformRotationSpeed = 15f;
        LocationTools.Rotate(_main, Vector3.up * platformRotationSpeed, duration);

        var topLightsTurnOnDuration = 5f;
        _topLights.ForEach(l => LocationTools.SetLightIntensity(l, _defaultTopLightsIntensity, topLightsTurnOnDuration));
        Timing.CallDelayed(topLightsTurnOnDuration, () =>
        {
            _ambientLights.ForEach(l => LocationTools.SetLightIntensity(l, _defaultAmbientLightsIntensity, 0.5f));
            _roomLights.ForEach(l => LocationTools.SetLightIntensity(l, _defaultRoomLightsIntensity, 0.5f));
        });
    }

    internal void OnStartingDangerPart(float duration)
    {
        _audioPlayer.RemoveAllClips();
        _audioPlayer.AddClip(AudioClipNames.DangerPart);

        _platformColliders.ForEach(o => o.NetworkPrimitiveFlags = PrimitiveFlags.None);
        _ponies.ForEach(p => p.enabled = false);

        foreach (var room in rooms)
            room.Light.NetworkLightIntensity = 0f;

        foreach (var room in safeRooms)
        {
            room.IsDoorLocked = false;
            room.OpenDoor();
            room.Light.NetworkLightIntensity = _defaultRoomLightsIntensity;
        }

        _topLights.ForEach(l => LocationTools.SwitchLightColors(l, _dangerPartColors, 0.15f, duration));
        _ambientLights.ForEach(l => LocationTools.SwitchLightColors(l, _dangerPartColors, 0.15f, duration));
    }

    internal void OnEndingDangerPart()
    {
        foreach (var room in rooms)
        {
            room.CloseDoor();
            room.IsDoorLocked = true;
        }

        var duration = 1.5f;
        _topLights.ForEach(l => LocationTools.SwitchLightColors(l, _dangerPartColors, 0.15f, duration));
        _ambientLights.ForEach(l => LocationTools.SwitchLightColors(l, _dangerPartColors, 0.15f, duration));
    }

    internal void OnEndingGameRound()
    {
        _topLights.ForEach(l => l.NetworkLightColor = _defaultLightsColor);
        _ambientLights.ForEach(l => l.NetworkLightColor = _defaultLightsColor);
        _allLights.ForEach(l => LocationTools.SetLightIntensity(l, 0f, 1f));

        _audioPlayer.RemoveAllClips();

        safeRooms.Clear();
    }

    #endregion
}
