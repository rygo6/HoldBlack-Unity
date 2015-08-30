﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class Enemy : MonoBehaviourBase
{
	
	#region Properties

	private int level
	{ 
		get { return m_Level; } 
		set { m_Level = value; } 
	}
	private int m_Level;
	
	private AnimationCurve3 positionHistoryCurve
	{ 
		get { return m_PositionHistoryCurve; } 
	}
	[SerializeField]
	private AnimationCurve3 m_PositionHistoryCurve = new AnimationCurve3();
	
	private AnimationCurve4 rotationHistoryCurve
	{ 
		get { return m_RotationHistoryCurve; } 
	}
	[SerializeField]
	private AnimationCurve4 m_RotationHistoryCurve = new AnimationCurve4();
	
	private AnimationCurve2 velocityHistoryCurve
	{ 
		get { return m_VelocityHistoryCurve; } 
	}
	[SerializeField]
	private AnimationCurve2 m_VelocityHistoryCurve = new AnimationCurve2();
	
	private AnimationCurve angularVelocityHistoryCurve
	{ 
		get { return m_AngularVelocityHistoryCurve; } 
	}
	[SerializeField]
	private AnimationCurve m_AngularVelocityHistoryCurve = new AnimationCurve();

	private AnimationCurve activeHistoryCurve
	{ 
		get { return m_ActiveHistoryCurve; } 
	}
	[SerializeField]
	private AnimationCurve m_ActiveHistoryCurve = new AnimationCurve();

	public RetireState retireState
	{
		get { return m_RetireState; }
		set	{ m_RetireState = value; }
	}
	private RetireState m_RetireState;

	public enum RetireState
	{
		NotRetired = 0,
		JustRetired = 1,
		PostJustRetired = 2,
		Retired = 3,
		JustUnRetired = 4,
		PostJustUnRetired = 5,
	}

	#endregion

	#region MonoBehaviour

	private void Awake()
	{
		
	}
	
	private void Start()
	{
		
	}

	public void OnCollisionEnter2D(Collision2D collision)
	{
		componentCache.spriteRenderer.color = Color.red;
	}

	public void OnCollisionExit2D(Collision2D collision)
	{
		componentCache.spriteRenderer.color = Color.white;
	}

	#endregion

	#region Methods

	public void RecordTransformHistory(float time)
	{
		switch (retireState)
		{
		case RetireState.NotRetired:
			positionHistoryCurve.AddKey(time, transform.position);
			rotationHistoryCurve.AddKey(time, transform.rotation);
			velocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.velocity);
			angularVelocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.angularVelocity);
			break;
		case RetireState.JustRetired:
			positionHistoryCurve.AddKey(time, transform.position, null, Mathf.Infinity);
			rotationHistoryCurve.AddKey(time, transform.rotation);
			velocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.velocity);
			angularVelocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.angularVelocity);
			retireState = RetireState.PostJustRetired;
			break;
		//PostJustRetired exists to ensure that atleast one full linear key gets placed
		//between the JustRetired and JustUnretired keys, otherwise the smoothing between these
		//may get messed up
		case RetireState.PostJustRetired:
			positionHistoryCurve.AddKey(time, transform.position, Mathf.Infinity, Mathf.Infinity);
			rotationHistoryCurve.AddKey(time, transform.rotation);
			velocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.velocity);
			angularVelocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.angularVelocity);
			retireState = RetireState.Retired;
			break;
		case RetireState.Retired:
			positionHistoryCurve.AddKey(time, transform.position, Mathf.Infinity, Mathf.Infinity);
			rotationHistoryCurve.AddKey(time, transform.rotation);
			velocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.velocity);
			angularVelocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.angularVelocity);
			break;
		case RetireState.JustUnRetired:
			Debug.Log("JustUnRetired "+time+" "+transform.position);
			positionHistoryCurve.AddKey(time, transform.position, Mathf.Infinity, null);
			rotationHistoryCurve.AddKey(time, transform.rotation);
			velocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.velocity);
			angularVelocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.angularVelocity);
			retireState = RetireState.PostJustUnRetired;
			break;
		//PostJustUnRetired exists because keys must be added through this 
		//method in order to not smooth the linear keyframes added in JustUnRetired state
		case RetireState.PostJustUnRetired:
			Debug.Log("PostJustUnRetired"+time+" "+transform.position);
			positionHistoryCurve.AddKey(time, transform.position, null, null);
			rotationHistoryCurve.AddKey(time, transform.rotation);
			velocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.velocity);
			angularVelocityHistoryCurve.AddKey(time, componentCache.rigidBody2D.angularVelocity);
			retireState = RetireState.NotRetired;
			break;
		}
	}
	
	public void EvaluateTransformHistory(float time)
	{
		transform.position = positionHistoryCurve.Evaluate(time);
		transform.rotation = rotationHistoryCurve.Evaluate(time);
		componentCache.rigidBody2D.velocity = velocityHistoryCurve.Evaluate(time);
		componentCache.rigidBody2D.angularVelocity = angularVelocityHistoryCurve.Evaluate(time);
	}
	
	public void DeleteHistoryAfterTime(float time)
	{
		positionHistoryCurve.DeleteAfterTime(time);
		rotationHistoryCurve.DeleteAfterTime(time);
		velocityHistoryCurve.DeleteAfterTime(time);
		angularVelocityHistoryCurve.DeleteAfterTime(time);
	}

	/// <summary>
	/// Holds physics object in place of actual transform
	/// until the fixedUpdate runs to update it's place
	/// in the physics simulation.
	/// </summary>
	public IEnumerator UpdatePhysicsToActualPosition()
	{
		Vector3 position = transform.position;
		Quaternion rotation = transform.rotation;
		yield return new WaitForFixedUpdate();
		transform.position = position;
		transform.rotation = rotation;		
	}

	#endregion

}