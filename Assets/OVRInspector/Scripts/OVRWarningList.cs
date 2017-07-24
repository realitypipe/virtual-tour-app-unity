using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// List of comfort warnings
/// </summary>
public class OVRWarningList : OVRDiscomfortWarningSource
{
    public OVRDiscomfortWarning.DiscomfortWarningType[] warnings;

    override public IEnumerable<OVRDiscomfortWarning.DiscomfortWarning> GetWarnings()
    {
        foreach (var w in warnings)
        {
            yield return new OVRDiscomfortWarning.DiscomfortWarning(w);
        }
    }
}
