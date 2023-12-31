using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class Player : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _ladderClimbSpeed = 6f;
    [SerializeField] private float _jumpHeight;
    [SerializeField] private float _gravity = -1f;
    [SerializeField] private float _pushForce = 6f;

    private Vector3 _moveDirection;
    private Vector3 _velocity;
    private float zHorizontal, yVertical;
    private float _yVelocity;
    private int _coins;
    private bool _isJumping;
    private bool _grabbedLedge;
    private bool _canClimbLadder;
    private bool _onLadder;
    private bool _isPushing;
    private bool _isRolling = false;
    private bool _isFrozen;

    private enum State { Normal, Rolling, Freeze}
    private State _state;

    PlayerLedgeChecker _activeLedge;
    LadderLedgeClimb _activeLadder;

    #region Properties
    public float Velocity => _velocity.x;
    public bool IsJumping => _isJumping;
    public bool CanClimbLadder
    {
        get => _canClimbLadder;
        set => _canClimbLadder = value;
    }

    public bool GrabbedLedge
    {
        get => _grabbedLedge;
        set => _grabbedLedge = value;
    }

    public bool OnLadder
    {
        get => _onLadder;
        set => _onLadder = value;
    }
    public bool IsPushing
    {
        get => _isPushing;
        set => _isPushing = value;
    }

    public bool IsFrozen => _isFrozen;


    #endregion


    CharacterController _controller;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();    
    }


    void Update()
    {
        zHorizontal = Input.GetAxisRaw("Horizontal");
        yVertical = Input.GetAxisRaw("Vertical");
        
        if (_grabbedLedge) return;
        switch (_state)
        {
            case State.Normal:
                _isFrozen = false;
                CalculateMovement();
                FlipPlayer();
                DodgeRollInput();
                break;

            case State.Rolling:
                _isFrozen = false;
                HandleRolling();
                break;
            case State.Freeze:
                _isFrozen = true;
                FreezePlayer();
                break;

        }

        if (Input.GetKeyUp(KeyCode.E) && _isPushing)
        {
            _isPushing = false;
        }
    }

    private void CalculateMovement()
    {
        if (_controller.isGrounded)
        {

            if (_isJumping)
            {
                _isJumping = false;
            }

            if (_onLadder)
            {
                _onLadder = false;
            }

            _moveDirection.z = zHorizontal;
            _velocity = _moveDirection * _moveSpeed;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _isJumping = true;
               // _yVelocity = 0;
                _yVelocity = _jumpHeight;
            }

            //Ladder
            if (Input.GetKeyDown(KeyCode.W) && _canClimbLadder)
            {
                _onLadder = true;
                _canClimbLadder = false;
            }

        }
        else
        {
            if (!OnLadder)
            {
                _yVelocity += _gravity;
            }
        }

        //Ladder
        if (_onLadder)
        {
            _yVelocity = 0;
            _moveDirection.y = yVertical;
            _velocity = _moveDirection * _ladderClimbSpeed;
        }

        if(!_onLadder) _velocity.y = _yVelocity;

        _controller.Move(_velocity * Time.deltaTime);
    }

    private void FlipPlayer()
    {
        if (_onLadder || _grabbedLedge) return;
        
        if (zHorizontal > 0)
        { 
            transform.localScale = new Vector3(3, 3, 3);
        }
        else if (zHorizontal < 0)
        {
            transform.localScale = new Vector3(3, 3, -3);
        }
        
    }

    private void DodgeRollInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _state = State.Rolling;
        }
    }

    private void HandleRolling()
    {
        if(!_isRolling)
            GetComponentInChildren<PlayerAnimation>().DodgePlayer();

        var dir = _moveSpeed;

        if (transform.localScale.z > 0)
        {
            if (dir < 0) dir *= -1;
        }
        else
        {
            if (dir > 0) dir *= -1;
        }
        _controller.SimpleMove(new Vector3(0,0, dir));
        _isRolling = true;
    }

    public void ChangeStateToNormal()
    {
        _state = State.Normal;
        _isRolling = false;

        if (_controller.enabled == false)
        {
            _controller.enabled = true;
        }
    }

    public void ChangeStateToFreeze()
    {
        _state = State.Freeze;
    }

    private void FreezePlayer()
    {
        _controller.enabled = false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Crate_Pushable"))
        {
            var movableObj = hit.collider.attachedRigidbody;
            if (movableObj.isKinematic) return;
            if (hit.moveDirection.y < -0.3f) return;

            if (Input.GetKey(KeyCode.E))
            {
                _isPushing = true;
                var pushDir = new Vector3(0, 0, hit.moveDirection.z);
                movableObj.velocity = pushDir * _pushForce;
            }
            else
            {
                _isPushing = false;
            }
        }
    }

    public void GrabLedge(Vector3 handPos, PlayerLedgeChecker currentLedge)
    {
        _controller.enabled = false;
        _grabbedLedge = true;
        transform.position = handPos;
        _isJumping = false;
        _activeLedge = currentLedge;
    }

    public void ClimbUpFromLedge()
    {
        _grabbedLedge = false;
        transform.position = _activeLedge.GetStandUpPos();
        _controller.enabled = true;
    }

    public void ClimbUpFromLadderAnimationStart(LadderLedgeClimb currentLadder)
    {
        _controller.enabled = false;
        GetComponentInChildren<PlayerAnimation>().ClimbUpFromLadder();
        _activeLadder = currentLadder;
    }

    public void ClimbUpFromLadder()
    {
        _onLadder = false;
        transform.position = _activeLadder.GetStandUpPoint();
        _controller.enabled = true;
    }

    public void AddCoins()
    {
        _coins++;
        UIManager.Instance.UpdateCoins();
    }

    public int GetTotalCoins()
    {
        return _coins;
    }

    public void RespawnPlayer(Transform respawnPos)
    {
        StartCoroutine(RespawnPlayerRoutine(respawnPos));
    }

    public IEnumerator RespawnPlayerRoutine(Transform respawnPos)
    {
        
        yield return UIManager.Instance.FadeIn(0.5f);
        ChangeStateToFreeze();
        yield return new WaitForSeconds(0.5f);
        transform.position = respawnPos.position;
        yield return UIManager.Instance.FadeOut(2f);
        ChangeStateToNormal();
    }

}//class
