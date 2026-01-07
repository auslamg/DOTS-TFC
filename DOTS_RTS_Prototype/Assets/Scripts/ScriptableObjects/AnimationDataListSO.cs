using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationDataListSO", menuName = "AnimationData/AnimationDataListSO")]
public class AnimationDataListSO : ScriptableObject
{
    public List<AnimationDataSO> animationDataSOList;
    public AnimationDataSO GetAnimationDataSO(AnimationDataSO.AnimationType animationType)
    {
        foreach (AnimationDataSO so in animationDataSOList)
        {
            if (so.animationType == animationType)
            {
                return so;
            }
        }
        Debug.LogError("Could not find AnimationData ScriptableObject for Animation type " + animationType);
        return null;
    }
}
