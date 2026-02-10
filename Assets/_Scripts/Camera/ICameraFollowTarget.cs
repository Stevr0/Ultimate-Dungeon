using UnityEngine;

/// <summary>
/// ICameraFollowTarget
/// -------------------
/// Tiny contract used to avoid reflection + serialization weirdness.
///
/// Any camera follow script that can be bound at runtime should implement this.
/// </summary>
public interface ICameraFollowTarget
{
    /// <summary>
    /// Bind the follow script to the given target transform.
    /// </summary>
    void SetTarget(Transform target);
}
