using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class ShineBrains : MonoBehaviour
{
    private const float DistanceBounds = 100f;
    private const float VelocityBounds = 100f;
    private const float AccelerationBounds = 10f;
    private const float AccelerationVariance = 20f;
    private const float JerkBounds = 10f;
    private float Velocity;
    private float Acceleration;
    private float Jerk;

    void Start()
    {
        transform.localPosition = Vector3.right * Rnd.Range(-DistanceBounds, DistanceBounds);
        Velocity = Rnd.Range(-VelocityBounds, VelocityBounds);
        Acceleration = Rnd.Range(-AccelerationBounds, AccelerationBounds);
        Jerk = Rnd.Range(-JerkBounds, JerkBounds);

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        while (true)
        {
            Jerk = Mathf.Clamp(Jerk + Rnd.Range(-JerkBounds, JerkBounds), -JerkBounds, JerkBounds);
            Acceleration = Mathf.Clamp(Acceleration + (Jerk * Time.deltaTime) - (transform.localPosition.x / 5f)
                + Rnd.Range(-AccelerationBounds / AccelerationVariance, AccelerationBounds / AccelerationVariance), -AccelerationBounds, AccelerationBounds);
            Velocity = Mathf.Clamp(Velocity + (Acceleration * Time.deltaTime), -VelocityBounds, VelocityBounds);
            transform.localPosition = Vector3.right * Mathf.Clamp(transform.localPosition.x + (Velocity * Time.deltaTime), -DistanceBounds, DistanceBounds);
            yield return null;
        }
    }
}
