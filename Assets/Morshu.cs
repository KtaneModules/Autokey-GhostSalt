using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Morshu : MonoBehaviour
{
    public Image MorshuRend;
    public Sprite[] MorshuSprites;

    private Coroutine MorshuAnimCoroutine;

    private void Awake()
    {
        MorshuRend.color = Color.clear;
    }

    public void Play()
    {
        if (MorshuAnimCoroutine != null)
            StopCoroutine(MorshuAnimCoroutine);
        MorshuAnimCoroutine = StartCoroutine(RunMorshu());
    }

    private IEnumerator RunMorshu()
    {
        MorshuRend.color = Color.white;
        for (int i = 0; i < 88; i++)
        {
            MorshuRend.sprite = MorshuSprites[i];
            yield return new WaitForSeconds(0.085f);
        }
        MorshuRend.color = Color.clear;
    }
}
