// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

/// <summary>
/// Simple utility script to destroy the gameobject after a set time.
/// </summary>
public class TimedDestroy : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

    public float m_lifeTime = 2.0f;

    // this seems to make motion stutter?
    //public float m_scaleToOneDuration = 0.1f;

    public float m_scaleToZeroDuration = 0.0f;

    Vector3 m_scale;
    float m_birthTime;

    void Start()
    {
        m_birthTime = Time.time;
        m_scale = transform.localScale;
    }

    void Update()
    {
        float age = Time.time - m_birthTime;

        if (age >= m_lifeTime)
        {
            Destroy(gameObject);
        }
        else if (age > m_lifeTime - m_scaleToZeroDuration)
        {
            transform.localScale = m_scale * (1.0f - (age - (m_lifeTime - m_scaleToZeroDuration)) / m_scaleToZeroDuration);
        }
        /*else if ( age < m_scaleToOneDuration && m_scaleToOneDuration > 0.0f )
        {
            transform.localScale = m_scale * age / m_scaleToOneDuration;
        }*/
        else
        {
            transform.localScale = m_scale;
        }
    }
}
