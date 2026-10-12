using MageDuel.Battle.Players;
using MageDuel.Networking;
using MageDuel.Networking.Contracts;
using UnityEngine;

namespace MageDuel.Battle.Camera
{
    public sealed class BattleCameraController : MonoBehaviour
    {
        private const int TerritoryZoneCount = 7;

        [Header("References")]
        [SerializeField] private UnityEngine.Camera _mainCamera;
        [SerializeField] private PlayerViewRegistry _playerViewRegistry;

        [Header("Fixed framing")]
        [SerializeField, Min(0.0f)] private float _longitudinalOffset = 80.0f;
        [SerializeField] private float _worldHeight = 77.0f;
        [SerializeField, Range(0.0f, 89.0f)] private float _downwardPitch = 39.0f;

        [Header("Territory framing")]
        [SerializeField] private AnimationCurve _heightOffsetByLookZone =
            CreateHeightOffsetCurve();
        [SerializeField] private AnimationCurve _pitchOffsetByLookZone =
            CreatePitchOffsetCurve();
        [SerializeField, Min(0.0f)] private float _zoneTransitionTime = 0.6f;

        private NetworkSession _networkSession;
        private float _worldX;
        private float _targetLookZone;
        private float _currentLookZone;
        private float _lookZoneVelocity;
        private float _fightZoneIndex;
        private float _arenaCenterZ;
        private float _zoneDepth;
        private bool _hasLookZone;
        private bool _hasFightZone;
        private bool _hasArenaGeometry;

        private void Awake()
        {
            _networkSession = NetworkSession.Instance;

            if (_playerViewRegistry == null)
            {
                _playerViewRegistry =
                    FindFirstObjectByType<PlayerViewRegistry>();
            }

            if (_mainCamera != null)
            {
                _worldX = _mainCamera.transform.position.x;
            }
        }

        private void OnEnable()
        {
            if (_networkSession != null)
            {
                _networkSession.BattleStarted +=
                    HandleBattleStarted;
                _networkSession.BattleStateReceived +=
                    HandleBattleStateReceived;
            }
        }

        private void OnDisable()
        {
            if (_networkSession != null)
            {
                _networkSession.BattleStarted -=
                    HandleBattleStarted;
                _networkSession.BattleStateReceived -=
                    HandleBattleStateReceived;
            }
        }

        private void LateUpdate()
        {
            if (_mainCamera == null ||
                _playerViewRegistry == null ||
                !_playerViewRegistry.TryGetLocalPlayerPosition(
                    out Vector3 localPlayerPosition) ||
                !TryGetLocalForwardSign(out float forwardSign))
            {
                return;
            }

            UpdateTargetLookZone(
                localPlayerPosition.z,
                forwardSign);
            UpdateCurrentLookZone();

            float height = _worldHeight +
                           _heightOffsetByLookZone.Evaluate(
                               _currentLookZone);
            float pitch = _downwardPitch +
                          _pitchOffsetByLookZone.Evaluate(
                              _currentLookZone);

            Vector3 position = CalculatePosition(
                localPlayerPosition,
                forwardSign,
                _worldX,
                height,
                _longitudinalOffset);

            Quaternion rotation = Quaternion.Euler(
                pitch,
                forwardSign > 0.0f ? 0.0f : 180.0f,
                0.0f);

            _mainCamera.transform.SetPositionAndRotation(
                position,
                rotation);
        }

        private void HandleBattleStarted(
            BattleStartedPayload payload)
        {
            _hasArenaGeometry = false;
            _hasFightZone = false;
            _hasLookZone = false;
            _lookZoneVelocity = 0.0f;

            if (payload == null)
            {
                return;
            }

            float arenaDepth =
                payload.arenaMaxZ - payload.arenaMinZ;
            if (arenaDepth <= Mathf.Epsilon)
            {
                return;
            }

            _arenaCenterZ =
                (payload.arenaMinZ + payload.arenaMaxZ) * 0.5f;
            _zoneDepth = arenaDepth / TerritoryZoneCount;
            _hasArenaGeometry = _zoneDepth > Mathf.Epsilon;
        }

        private void HandleBattleStateReceived(
            BattleStatePayload payload)
        {
            if (payload == null ||
                string.IsNullOrEmpty(payload.blueZone) ||
                string.IsNullOrEmpty(payload.redZone) ||
                !TryGetLocalForwardSign(out _))
            {
                return;
            }

            var protocol = _networkSession.Config.Runtime.Protocol;
            if (payload.blueZone == protocol.UnknownZone ||
                payload.redZone == protocol.UnknownZone)
            {
                return;
            }

            int blueZoneIndex = protocol.ZoneIndex(payload.blueZone);
            int redZoneIndex = protocol.ZoneIndex(payload.redZone);
            _fightZoneIndex =
                (blueZoneIndex + redZoneIndex) * 0.5f;
            _hasFightZone = true;
        }

        private void UpdateTargetLookZone(
            float localPlayerZ,
            float forwardSign)
        {
            if (!_hasArenaGeometry || !_hasFightZone)
            {
                _targetLookZone = 0.0f;
            }
            else
            {
                _targetLookZone = CalculateLookZone(
                    localPlayerZ,
                    _arenaCenterZ,
                    _zoneDepth,
                    _fightZoneIndex,
                    forwardSign);
            }

            if (_hasLookZone)
            {
                return;
            }

            _currentLookZone = _targetLookZone;
            _lookZoneVelocity = 0.0f;
            _hasLookZone = true;
        }

        private void UpdateCurrentLookZone()
        {
            if (!_hasLookZone)
            {
                _currentLookZone = 0.0f;
                return;
            }

            if (_zoneTransitionTime <= Mathf.Epsilon)
            {
                _currentLookZone = _targetLookZone;
                _lookZoneVelocity = 0.0f;
                return;
            }

            _currentLookZone = Mathf.SmoothDamp(
                _currentLookZone,
                _targetLookZone,
                ref _lookZoneVelocity,
                _zoneTransitionTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        private bool TryGetLocalForwardSign(out float forwardSign)
        {
            forwardSign = 0.0f;

            NetworkSession session = NetworkSession.Instance;
            if (session == null ||
                session.CurrentBattleAssignment == null)
            {
                return false;
            }

            string localTeam =
                session.CurrentBattleAssignment.LocalTeam;
            if (localTeam == session.Config.Runtime.Protocol.BlueTeam)
            {
                forwardSign = 1.0f;
                return true;
            }

            if (localTeam == session.Config.Runtime.Protocol.RedTeam)
            {
                forwardSign = -1.0f;
                return true;
            }

            return false;
        }

        private static float CalculateLookZone(
            float localPlayerZ,
            float arenaCenterZ,
            float zoneDepth,
            float fightZoneIndex,
            float forwardSign)
        {
            float localPlayerZone =
                (localPlayerZ - arenaCenterZ) / zoneDepth;

            return (fightZoneIndex - localPlayerZone) *
                   forwardSign - 1.0f;
        }

        private static Vector3 CalculatePosition(
            Vector3 localPlayerPosition,
            float forwardSign,
            float worldX,
            float worldHeight,
            float longitudinalOffset)
        {
            return new Vector3(
                worldX,
                worldHeight,
                localPlayerPosition.z -
                forwardSign * longitudinalOffset);
        }

        private static AnimationCurve CreateHeightOffsetCurve()
        {
            return new AnimationCurve(
                new Keyframe(-1.0f, 20.0f, -20.0f, -20.0f),
                new Keyframe(0.0f, 0.0f, -20.0f, -20.0f),
                new Keyframe(1.0f, -20.0f, -20.0f, -10.0f),
                new Keyframe(2.0f, -30.0f, -10.0f, -10.0f));
        }

        private static AnimationCurve CreatePitchOffsetCurve()
        {
            return new AnimationCurve(
                new Keyframe(-1.0f, 10.0f, -10.0f, -10.0f),
                new Keyframe(0.0f, 0.0f, -10.0f, -10.0f),
                new Keyframe(1.0f, -10.0f, -10.0f, -5.0f),
                new Keyframe(2.0f, -15.0f, -5.0f, -5.0f));
        }
    }
}
