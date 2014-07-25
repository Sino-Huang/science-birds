﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bird : Character {

    private int _nextParticleTrajectory;
    
    private Vector3  _selectPosition;

    public float _dragRadius = 1.0f;
    public float _dragSpeed = 1.0f;
    public float _launchGravity = 1.0f;
    public float _trajectoryParticleFrequency = 0.5f;
    public float _jumpForce;
    public float _maxTimeToJump;

    public Vector2 _launchForce;
    public Transform _slingshot;
    public Transform _slingshotBase;
    
    public GameObject[] _trajectoryParticles;

    public bool JumpToSlingshot{ get; set; }
    public bool OutOfSlingShot{ get; set; }

	public override void Start ()
    {
		base.Start();

        _slingshotBase.active = false;

        float nextJumpDelay = Random.Range(0.0f, _maxTimeToJump);
        Invoke("IdleJump", nextJumpDelay + 1.0f);
    }

    void Update()
    {
        if(IsFlying() && !OutOfSlingShot)

            DragBird(transform.position);
    }

    void IdleJump()
    {
        if(JumpToSlingshot)
            return;

        if(IsIdle() && rigidbody2D.gravityScale > 0f)

            rigidbody2D.AddForce(Vector2.up * _jumpForce);

        float nextJumpDelay = Random.Range(0.0f, _maxTimeToJump);
        Invoke("IdleJump", nextJumpDelay + 1.0f);
    }

    void DropTrajectoryParticle()
    {
        _nextParticleTrajectory = (_nextParticleTrajectory + 1) % _trajectoryParticles.Length;

        GameObject particle = (GameObject) Instantiate(_trajectoryParticles[_nextParticleTrajectory], transform.position, Quaternion.identity);
        particle.transform.parent = GameObject.Find("Foreground/Effects").transform;
        particle.name = name;
    }

    void RemoveLastTrajectoryParticle(string birdName)
    {
        int lastBirdIndex = int.Parse(name.Substring(name.Length - 1)) - 1;

        if(lastBirdIndex > 0)
        {
            string lastBirdName = birdName.Remove(birdName.Length - 1, 1);
            lastBirdName = lastBirdName + lastBirdIndex;

            GameObject effects = GameObject.Find("Foreground/Effects");

            foreach (Transform child in effects.transform)
            {
                if(child.gameObject.name == lastBirdName)

                    Destroy(child.gameObject);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(IsFlying())
        {
            CancelInvoke("DropTrajectoryParticle");
            RemoveLastTrajectoryParticle(name);

            Invoke("Die", _timeToDie);
            _animator.Play("die", 0, 0f);
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider.tag == "Slingshot")
        {
            if(JumpToSlingshot)
				_slingshotBase.active = false;
        }
    }

    void OnTriggerStay2D(Collider2D collider)
    {
        if(collider.tag == "Slingshot")
        {
            if(JumpToSlingshot)
                _slingshotBase.active = false;

            if(IsFlying())
            {
                Vector3 slingBasePos = _selectPosition;
                slingBasePos.z = transform.position.z + 0.5f;
                _slingshotBase.transform.position = slingBasePos;

                _slingshotBase.transform.rotation = Quaternion.Euler(_slingshotBase.transform.rotation.x,
                                                                     _slingshotBase.transform.rotation.y, 0f);

                OutOfSlingShot = true;
            }
        }
    }
	
    public bool IsFlying()
    {
        return _animator.GetCurrentAnimatorStateInfo(0).IsName("flying");
    }

    public bool IsSelected()
    {
        return _selectPosition != Vector3.zero;
    }

    public void SelectBird()
    {
        _slingshotBase.active = true;
        _selectPosition = transform.position;
        _animator.Play("selected", 0, 0f);
    }

    public void SetBirdOnSlingshot(Vector3 endPosition)
    {
        transform.position = Vector3.MoveTowards(transform.position, endPosition, _dragSpeed * Time.deltaTime);
    }

	public void DragBird(Vector3 dragPosition)
	{
        float deltaPosFromSlingshot = Vector3.Distance(dragPosition, _selectPosition);

        // Lock bird movement inside a circle
        if(deltaPosFromSlingshot > _dragRadius)
            dragPosition = (dragPosition - _selectPosition).normalized * _dragRadius + _selectPosition;

        transform.position = Vector3.Lerp (transform.position, dragPosition, Time.deltaTime * _dragSpeed);

		// Slingshot base look to slingshot
        Vector3 dist = _slingshotBase.transform.position - _selectPosition;
        float angle = Mathf.Atan2(dist.y, dist.x) * Mathf.Rad2Deg;
        _slingshotBase.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Slingshot base rotate around the selected point
        CircleCollider2D col = GetComponent<CircleCollider2D>();
		_slingshotBase.transform.position = (transform.position - _selectPosition).normalized * col.radius + transform.position;
	}

	public void LaunchBird()
	{
        Vector2 deltaPosFromSlingshot = transform.position - _selectPosition;
		_animator.Play("flying", 0, 0f);

		// The bird starts with no gravity, so we must set it
		rigidbody2D.gravityScale = _launchGravity;
		rigidbody2D.velocity = new Vector2(_launchForce.x * -deltaPosFromSlingshot.x,
                                           _launchForce.y * -deltaPosFromSlingshot.y) * Time.deltaTime;

        InvokeRepeating("DropTrajectoryParticle", 0.1f, _trajectoryParticleFrequency / Mathf.Abs(rigidbody2D.velocity.x));
	}
}
