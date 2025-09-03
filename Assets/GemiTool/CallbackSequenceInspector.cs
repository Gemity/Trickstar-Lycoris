using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallbackSequence
{
    public string name;
    public UnityEngine.Events.UnityEvent callback;
    public float duration;
}

public class CallbackSequenceInspector : MonoBehaviour
{
    [SerializeField] private CallbackSequence[] _callbacks;

    [Space(20)]
    public UnityEngine.Events.UnityEvent onComplete;
    private Coroutine _coroutine;
    public void Play()
    {
        _coroutine = StartCoroutine(Co_Play());
    }

    public void Stop()
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);
    }

    private IEnumerator Co_Play()
    {
        foreach(var i in _callbacks)
        {
            i.callback?.Invoke();
            yield return new WaitForSeconds(i.duration);
        }

        onComplete?.Invoke();
    }
}
