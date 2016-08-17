using System;

public class VWeakReference
{
	int targetHashCode;
	WeakReference weakReferenceToTarget;

	void SetTarget(object target)
	{
		targetHashCode = target != null ? target.GetHashCode() : -1; // maybe make-so: this gets refreshed each time GetHashCode is called (since GC can move target, and therefore change its hash-code)
		weakReferenceToTarget = new WeakReference(target);
	}
	public VWeakReference(object target) { SetTarget(target); }

	public object Target
	{
		get { return weakReferenceToTarget.Target; }
		set { SetTarget(value); }
	}

	public bool IsAlive { get { return weakReferenceToTarget.IsAlive; } }

	public override int GetHashCode() { return targetHashCode; }
	public override bool Equals(object obj) { return targetHashCode == obj.GetHashCode(); } // maybe make-so: the actual objects are compared, since hash-codes can overlap apparently
}