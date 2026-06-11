using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinoGame.Components
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class BoneTurnComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform searchRoot;
        [SerializeField] private Transform[] spineBones;
        [SerializeField] private Transform[] neckBones;
        [SerializeField] private Transform[] tailBones;

        [Header("Turn Lean")]
        [SerializeField] private float maxSpineLean = 14f;
        [SerializeField] private float leanSmoothTime = 0.35f;
        [SerializeField] private float maxNeckYawTotal = 18f;
        [SerializeField] private float maxSpineRollTotal = 8f;
        [SerializeField] private float maxTailYawTotal = 14f;
        [SerializeField] private float speedLeanReference = 6f;
        [SerializeField] private float leanAngleReference = 90f;

        private float spineLean;
        private float spineLeanVelocity;

        public float SpineLean => spineLean;

        private void Awake()
        {
            if (NeedsAutoAssign())
                AutoAssignBones();
        }

        private void LateUpdate()
        {
            ApplyBoneRotations();
        }

        public void TickTurn(float signedLeanAngle, float moveSpeed, float deltaTime)
        {
            if (moveSpeed < 0.15f || maxSpineLean <= 0.01f)
            {
                spineLean = Mathf.SmoothDamp(spineLean, 0f, ref spineLeanVelocity, leanSmoothTime, 90f, deltaTime);
                return;
            }

            float speedFactor = speedLeanReference > 0.01f
                ? Mathf.Clamp01(moveSpeed / speedLeanReference)
                : 0f;

            float angleReference = leanAngleReference > 1f ? leanAngleReference : 90f;
            float normalizedLean = Mathf.Clamp(signedLeanAngle / angleReference, -1f, 1f);
            float targetLean = normalizedLean * maxSpineLean * speedFactor;
            spineLean = Mathf.SmoothDamp(spineLean, targetLean, ref spineLeanVelocity, leanSmoothTime, 90f, deltaTime);
        }

        public void ApplyBoneRotations()
        {
            if (Mathf.Abs(spineLean) < 0.01f)
                return;

            if (maxSpineLean <= 0.01f && maxNeckYawTotal <= 0.01f && maxSpineRollTotal <= 0.01f && maxTailYawTotal <= 0.01f)
                return;

            float lean01 = maxSpineLean > 0.01f ? spineLean / maxSpineLean : Mathf.Sign(spineLean);
            float spineRoll = lean01 * maxSpineRollTotal;
            float neckYaw = lean01 * maxNeckYawTotal;
            float tailRoll = -lean01 * maxTailYawTotal;

            ApplyChainLean(spineBones, neckYaw * 0.2f, spineRoll);
            ApplyChainLean(neckBones, neckYaw, spineRoll * 0.25f);
            ApplyChainLean(tailBones, -neckYaw * 0.15f, tailRoll);
        }

        private static void ApplyChainLean(Transform[] bones, float yaw, float roll)
        {
            if (bones == null || bones.Length == 0)
                return;

            if (Mathf.Abs(yaw) < 0.001f && Mathf.Abs(roll) < 0.001f)
                return;

            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null)
                    continue;

                float weight = (i + 1) / (float)bones.Length;
                bones[i].rotation *= Quaternion.Euler(0f, yaw * weight, roll * weight);
            }
        }

        [ContextMenu("Auto Assign Bones")]
        public bool AutoAssignBones()
        {
            Transform root = ResolveSearchRoot();

            spineBones = FindIndexedBones(root, "spine");
            neckBones = FindIndexedBones(root, "neck");
            tailBones = FindIndexedBones(root, "tail");

            bool assigned = spineBones.Length > 0 || neckBones.Length > 0 || tailBones.Length > 0;

#if UNITY_EDITOR
            if (assigned)
                UnityEditor.EditorUtility.SetDirty(this);
#endif

            return assigned;
        }

        private bool NeedsAutoAssign()
        {
            return IsEmpty(spineBones) && IsEmpty(neckBones) && IsEmpty(tailBones);
        }

        private Transform ResolveSearchRoot()
        {
            if (searchRoot != null)
                return searchRoot;

            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
                return animator.transform;

            return transform;
        }

        private static bool IsEmpty(Transform[] bones)
        {
            return bones == null || bones.Length == 0;
        }

        private static Transform[] FindIndexedBones(Transform root, string prefix)
        {
            var matches = new List<(int index, Transform bone)>();

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!TryParseIndexedBoneName(child.name, prefix, out int index))
                    continue;

                matches.Add((index, child));
            }

            matches.Sort((a, b) => a.index.CompareTo(b.index));

            var unique = new List<Transform>();
            int lastIndex = int.MinValue;

            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].index == lastIndex)
                    continue;

                lastIndex = matches[i].index;
                unique.Add(matches[i].bone);
            }

            return unique.ToArray();
        }

        private static bool TryParseIndexedBoneName(string boneName, string prefix, out int index)
        {
            index = -1;

            if (string.IsNullOrWhiteSpace(boneName))
                return false;

            if (!boneName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string suffix = boneName.Substring(prefix.Length);
            return int.TryParse(suffix, out index);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (NeedsAutoAssign())
                AutoAssignBones();
        }

        private void Reset()
        {
            AutoAssignBones();
        }
#endif
    }
}
