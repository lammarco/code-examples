using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GrappleAnchor : MonoBehaviour
{
	public delegate void GrapHandler(Rigidbody2D attached);
	private event GrapHandler OnAttach;
	
	public Action<bool, Vector2> callback;
	public Color shootColor, restColor;
	public float shootMass, restMass, detachForce;
	[Range(0,1)]
	public float shootG;
	[SerializeField]
	private LayerMask platformMask;
	
	private static readonly string grappleAnchorTag = "Grap Anchor";
	
	private SpriteRenderer sprite;
	private SpringJoint2D sJ;
	private Collider2D col;
	//private Vector2 contactNormal;
	public Rigidbody2D rb2d{get; private set;}
	public Collider2D connected{get; private set;}
	private ConveyorBeltEffector2D conveyor;
	private Transform trans;
	//private float conveyorSpeed;
	private int conveyorIndexCache;
	
	private float t;
	
	public void SubscribeAttach( GrapHandler h )
		{OnAttach += h;}
	
	void Awake(){
		col = GetComponent<Collider2D>();
		rb2d = GetComponent<Rigidbody2D>();
		sJ = GetComponent<SpringJoint2D>();
		sprite = GetComponent<SpriteRenderer>();
		trans = this.transform;
	}
	
	void OnEnable(){
		Detach();
		rb2d.mass = shootMass;
		rb2d.gravityScale = shootG;
		sprite.color = shootColor;
	}
	
	public void Detach()
	{
		Vector2 rand = Quaternion.Euler(0,0,UnityEngine.Random.value*360) * Vector2.right;
		Vector2 normal = Vector2.zero;
		if(connected != null){
			ColliderDistance2D colDist = col.Distance( connected );
			if( colDist.isValid )
				normal = colDist.normal;
		}
		rand = rand*0.75f + normal;
		rb2d.AddForce(rand * rb2d.mass * detachForce, ForceMode2D.Impulse);
		
		connected = null;
		conveyor = null;
		conveyorIndexCache = -1;
		sJ.enabled = false;
		rb2d.isKinematic = false;
		rb2d.constraints = RigidbodyConstraints2D.None;
		rb2d.bodyType = RigidbodyType2D.Dynamic;
		col.isTrigger = false;
	}
	
	Rigidbody2D GetRb2d(Collider2D other){
		Rigidbody2D rb2d = other.attachedRigidbody;
		return (rb2d != null && rb2d.bodyType != RigidbodyType2D.Static)
				? rb2d : null;
			
	}
	
	void AttachRoutine(Collider2D other){
		col.isTrigger = true;
		sprite.color = restColor;
		connected = other;
		sJ.connectedBody = GetRb2d(other);
		rb2d.mass = restMass;
	}
	
    void OnCollisionEnter2D(Collision2D collision){
		if(sprite.color == restColor)
			return; //already attached once
		
		if(((1 << collision.gameObject.layer) | platformMask) == platformMask){
			ContactPoint2D contact = collision.GetContact(0);
			rb2d.position = contact.point;
			AttachRoutine(collision.collider);
			
			if(connected.GetComponent<ConveyorBeltEffector2D>() is ConveyorBeltEffector2D belt){
				rb2d.isKinematic = true;
				conveyor = belt;
				rb2d.velocity = Vector2.zero;
				OnAttach?.Invoke(rb2d);
			}else if(sJ.connectedBody != null){
				sJ.enabled = true;
				OnAttach?.Invoke(collision.rigidbody); //special case: look at anchor instead of grapple hook
			}else{
				rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
				rb2d.bodyType = RigidbodyType2D.Static;
				OnAttach?.Invoke(rb2d);
			}
		}
	}
	
	//special handling for Anchor/Target-objects
	void OnTriggerEnter2D(Collider2D other){
		if(sprite.color == restColor)
			return; //already attached once
		
		if(other.CompareTag(grappleAnchorTag) && other.attachedRigidbody != null){
			AttachRoutine(other);
			StartCoroutine(MoveTowards());
			//do not activate SpringJoint2D until finished
			OnAttach?.Invoke(other.attachedRigidbody);
		}
	}
	
	public void FixedUpdate()
	{
		if(conveyor != null){
			FollowConveyor();
		}
	}
	
	private void FollowConveyor()
	{
		ColliderDistance2D colDist = col.Distance( connected );
		//move along conveyor belt, stop if not overlapped / reach the end stop
		float d = colDist.distance;
		Vector2 surf = conveyor.GetEffectiveDirection( colDist.pointB, ref conveyorIndexCache, 2*d*d );
		if( surf == Vector2.zero ){
			conveyor = null;
			rb2d.velocity = Vector2.zero;
			rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
			rb2d.bodyType = RigidbodyType2D.Static;
		}else{
			rb2d.velocity = (rb2d.velocity + surf)/2; //lerp halfway
		}
	}
	
	IEnumerator MoveTowards(){
		rb2d.isKinematic = true;
		rb2d.velocity = Vector2.zero;
		Transform other = connected.transform;
		t = 1;
		while(t > 0.1f){
			trans.position = Vector3.Lerp(other.position,trans.position,t);
			t -= t/30;
			yield return null;
		}
		trans.position = other.position;
		rb2d.isKinematic = false;
		// enable joint if rope is still attached (can break during coroutine)
		if(connected != null){
			if( sJ.connectedBody != null )
				sJ.enabled = true;
			else{ //implies anchor is static = freeze anchor instead
				rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
				rb2d.bodyType = RigidbodyType2D.Static;
			}
		}
	}
	
	void OnDisable(){
		StopAllCoroutines();
		rb2d.isKinematic = false;
	}
}
