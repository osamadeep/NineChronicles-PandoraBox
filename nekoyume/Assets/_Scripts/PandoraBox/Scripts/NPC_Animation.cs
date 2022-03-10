using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Animation : MonoBehaviour
{
    public enum Type
    {
        // todo: `Appear`와 `Disappear`는 없어질 예정.
        Appear,
        Disappear,
        Standing,
        StandingToIdle,
        Idle,
        Touch,
        Run,
        Attack,
        Casting,
        CastingAttack,
        CriticalAttack,
        Hit,
        Die,
        Win,
        Win_02,
        Win_03,
        Greeting,
        Emotion,
        Skill_01,
        Skill_02,
        TurnOver_01,
        TurnOver_02
    }

    public Type AnimationType;
    Animator animator;

    // Start is called before the first frame update

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        animator.Play(nameof(AnimationType), 0, 0f);
    }

}
