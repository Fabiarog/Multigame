using Godot;
using System;
using GameHub.Core.Systems;

namespace GameHub.Core.Visuals;

/// <summary>
/// States representing the character's mental focus at the table.
/// </summary>
public enum AttentionState
{
    Relaxed,
    WatchingPlayer,
    WatchingCard,
    Thinking,
    Challenging,
    Celebrating,
    Defeated
}

/// <summary>
/// Native Godot SkeletonModifier3D that provides anatomical, procedural look-at,
/// eye-head coordination, micro-saccades and rhythmic breathing without breaking authored animation clips.
/// Enforces strict anatomical limits: Yaw [-48°, +48°] and Pitch [-14°, +14°].
/// </summary>
public partial class ProceduralAttentionModifier : SkeletonModifier3D
{
    public const float MaxYawDegrees = 48.0f;
    public const float MaxPitchDegrees = 14.0f;

    public int SeatIndex { get; set; } = 0;
    public AttentionState State { get; set; } = AttentionState.Relaxed;
    public Vector3 LookTargetWorld { get; set; } = Vector3.Zero;
    public Node3D TargetNode { get; set; } = null;
    public float AttentionWeight { get; set; } = 1.0f;

    private int _headBone = -1;
    private int _neckBone = -1;
    private int _chestBone = -1;

    // Smoothed procedural rotation offsets
    private float _currentYaw = 0f;
    private float _currentPitch = 0f;
    private float _targetYaw = 0f;
    private float _targetPitch = 0f;

    // Organic micro-behaviors
    private float _breathingPhase = 0f;
    private float _saccadeTimer = 0f;
    private float _saccadeOffsetX = 0f;
    private float _saccadeOffsetY = 0f;
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        _breathingPhase = _rng.RandfRange(0f, Mathf.Tau);
        _saccadeTimer = _rng.RandfRange(1.5f, 3.5f);
        DiscoverBones();
    }

    public void DiscoverBones()
    {
        var sk = GetSkeleton();
        if (sk == null) return;

        _headBone = -1;
        _neckBone = -1;
        _chestBone = -1;

        for (int i = 0; i < sk.GetBoneCount(); i++)
        {
            string name = sk.GetBoneName(i);
            if (_headBone < 0 && (name.Equals("Head", StringComparison.OrdinalIgnoreCase) ||
                                 name.StartsWith("Head", StringComparison.OrdinalIgnoreCase)))
            {
                _headBone = i;
            }
            else if (_neckBone < 0 && (name.Equals("Neck", StringComparison.OrdinalIgnoreCase) ||
                                       name.StartsWith("Neck", StringComparison.OrdinalIgnoreCase)))
            {
                _neckBone = i;
            }
            else if (_chestBone < 0 && (name.Equals("Chest", StringComparison.OrdinalIgnoreCase) ||
                                        name.Equals("Spine", StringComparison.OrdinalIgnoreCase)))
            {
                _chestBone = i;
            }
        }
    }

    public void LookAtWorld(Vector3 worldTarget, AttentionState state = AttentionState.WatchingCard)
    {
        LookTargetWorld = worldTarget;
        TargetNode = null;
        State = state;
    }

    public void LookAtNode(Node3D targetNode, AttentionState state = AttentionState.WatchingPlayer)
    {
        TargetNode = targetNode;
        State = state;
    }

    public void ResetAttention(AttentionState state = AttentionState.Relaxed)
    {
        LookTargetWorld = Vector3.Zero;
        TargetNode = null;
        State = state;
    }

    public override void _ProcessModificationWithDelta(double delta)
    {
        var sk = GetSkeleton();
        if (sk == null) return;

        if (_headBone < 0 || _neckBone < 0)
        {
            DiscoverBones();
            if (_headBone < 0) return;
        }

        float dt = (float)delta;
        bool motionAllowed = SettingsManager.Instance?.ReduceMotion != true;

        if (!motionAllowed || AttentionWeight <= 0.001f || !Active)
        {
            // Smoothly decay existing rotation to identity to avoid snapping
            _currentYaw = Mathf.Lerp(_currentYaw, 0f, 1f - Mathf.Exp(-10f * dt));
            _currentPitch = Mathf.Lerp(_currentPitch, 0f, 1f - Mathf.Exp(-10f * dt));
            if (Mathf.Abs(_currentYaw) < 0.001f && Mathf.Abs(_currentPitch) < 0.001f)
                return;
        }
        else
        {
            // 1. Update micro-movements: continuous breathing cycle (~0.22 Hz)
            _breathingPhase += dt * 1.4f;
            float breathOffset = Mathf.Sin(_breathingPhase) * 0.006f; // ~0.35 degrees pitch oscillation

            // 2. Micro-saccades: slight ocular/cervical micro-shifts every 2 to 4 seconds
            _saccadeTimer -= dt;
            if (_saccadeTimer <= 0f)
            {
                _saccadeTimer = _rng.RandfRange(2.0f, 4.5f);
                _saccadeOffsetX = _rng.RandfRange(-0.02f, 0.02f); // ~1.1 degrees
                _saccadeOffsetY = _rng.RandfRange(-0.015f, 0.015f);
            }

            // 3. Resolve target world position
            Vector3 worldTarget = LookTargetWorld;
            if (TargetNode != null && IsInstanceValid(TargetNode))
            {
                worldTarget = TargetNode.GlobalPosition;
            }

            // Default target when in relaxed mode or no target: look naturally toward table center
            if (worldTarget == Vector3.Zero)
            {
                worldTarget = new Vector3(0f, 0.15f, 0f);
            }

            // 4. Transform target into skeleton local coordinate space
            Vector3 localTarget = sk.GlobalTransform.AffineInverse() * worldTarget;
            Vector3 headLocalOrigin = sk.GetBoneGlobalPose(_headBone).Origin;
            Vector3 toTarget = (localTarget - headLocalOrigin);

            // 5. Calculate Yaw and Pitch in bone space
            // In character local space, +Z is forward, +Y is up, +X is right
            float rawYaw = Mathf.Atan2(toTarget.X, toTarget.Z);
            float horizontalDist = Mathf.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);
            float rawPitch = -Mathf.Atan2(toTarget.Y, horizontalDist);

            // 6. Apply state biases
            switch (State)
            {
                case AttentionState.Thinking:
                    // Head tilted down towards cards/table
                    rawPitch -= 0.10f; // -5.7 degrees down
                    rawYaw += 0.06f;   // slight introspective tilt
                    break;
                case AttentionState.Challenging:
                    // Chin slightly raised, intense forward lock
                    rawPitch += 0.05f;
                    _saccadeOffsetX *= 0.2f;
                    _saccadeOffsetY *= 0.2f;
                    break;
                case AttentionState.Celebrating:
                    // Looking up proudly
                    rawPitch += 0.12f; // ~7 degrees up
                    break;
                case AttentionState.Defeated:
                    // Crestfallen, looking down
                    rawPitch -= 0.16f; // ~9 degrees down
                    break;
            }

            // 7. Strictly clamp within anatomical limits: Yaw ±48°, Pitch ±14°
            float maxYawRad = Mathf.DegToRad(MaxYawDegrees);
            float maxPitchRad = Mathf.DegToRad(MaxPitchDegrees);

            _targetYaw = Mathf.Clamp(rawYaw + _saccadeOffsetX, -maxYawRad, maxYawRad);
            _targetPitch = Mathf.Clamp(rawPitch + breathOffset + _saccadeOffsetY, -maxPitchRad, maxPitchRad);

            // 8. Smooth interpolation (exponential slerp/damping rate ~8.5)
            float lerpFactor = 1f - Mathf.Exp(-8.5f * dt);
            _currentYaw = Mathf.Lerp(_currentYaw, _targetYaw, lerpFactor);
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, lerpFactor);
        }

        // 9. Distribute procedural rotation organically:
        // Neck absorbs 30% of total cervical rotation, Head absorbs 70%
        float neckYaw = _currentYaw * 0.30f * AttentionWeight;
        float neckPitch = _currentPitch * 0.30f * AttentionWeight;
        float headYaw = _currentYaw * 0.70f * AttentionWeight;
        float headPitch = _currentPitch * 0.70f * AttentionWeight;

        // Construct procedural rotation Quaternions (Y = yaw, X = pitch)
        var neckProcRot = new Quaternion(Vector3.Up, neckYaw) * new Quaternion(Vector3.Right, neckPitch);
        var headProcRot = new Quaternion(Vector3.Up, headYaw) * new Quaternion(Vector3.Right, headPitch);

        // Apply on top of evaluated animation pose
        if (_neckBone >= 0)
        {
            var animNeckPose = sk.GetBonePoseRotation(_neckBone);
            sk.SetBonePoseRotation(_neckBone, animNeckPose * neckProcRot);
        }

        var animHeadPose = sk.GetBonePoseRotation(_headBone);
        sk.SetBonePoseRotation(_headBone, animHeadPose * headProcRot);
    }
}
