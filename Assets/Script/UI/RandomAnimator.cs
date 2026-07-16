using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private readonly List<string> stateNames = new();

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        CacheAnimations();
    }

    private void OnEnable()
    {
        PlayRandomAnimation();
    }

    /// <summary>
    /// Lấy toàn bộ AnimationClip trong AnimatorController.
    /// </summary>
    private void CacheAnimations()
    {
        stateNames.Clear();

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            // Tránh clip bị trùng
            if (!stateNames.Contains(clip.name))
                stateNames.Add(clip.name);
        }
    }

    public void PlayRandomAnimation()
    {
        if (stateNames.Count == 0)
            return;

        int index = Random.Range(0, stateNames.Count);
        animator.Play(stateNames[index], 0, 0f);
    }

    public void PlayRandomAnimationNoRepeat()
    {
        if (stateNames.Count <= 1)
        {
            PlayRandomAnimation();
            return;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

        int currentIndex = -1;

        for (int i = 0; i < stateNames.Count; i++)
        {
            if (current.IsName(stateNames[i]))
            {
                currentIndex = i;
                break;
            }
        }

        int newIndex;
        do
        {
            newIndex = Random.Range(0, stateNames.Count);
        }
        while (newIndex == currentIndex);

        animator.Play(stateNames[newIndex], 0, 0f);
    }

    public void PlayRandomLoop()
    {
        StopAllCoroutines();
        StartCoroutine(RandomLoop());
    }

    private IEnumerator RandomLoop()
    {
        while (true)
        {
            PlayRandomAnimationNoRepeat();

            yield return null;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(state.length);
        }
    }
}