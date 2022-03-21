using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))] //如果没有刚体 强行加刚体
public class Player : MonoBehaviour
{
    [Header("碰撞盒")] 
    private BoxCollider2D AttackBox; //攻击碰撞盒子

    [Header("动画参数")] 
    private Animator anim; //动画状态机
    [SerializeField] private string currentState;
     [SerializeField] private bool isLocked; //当前动画状态是否能锁定 锁定时不能被更改
    private const string idle = "Player_idle";
    private const string jump = "Player_jump";
    private const string fall = "Player_fall";
    private const string move = "Player_move";
    private const string attack = "Player_attack";
     private const string dash = "Player_dash";

    [Header("移动参数")]
    [SerializeField]private float speed; //移动速度
    [SerializeField] private float jumpForce; //跳跃力
    [SerializeField] private float inputFaceDirection; //面朝方向 正右负左 输入才会有
    [SerializeField] private int faceDirectionRaw; //面朝方向 正右负左 用于记录方向 无输入时默认向右
    [SerializeField] private bool isMoving; //是否正在移动
    private Rigidbody2D rb;
    
    [Header("跳跃检测参数")]
    [SerializeField]private bool isGrounded = false;  //是否在地面上
    [SerializeField]private Transform botPos; //地面判定点
    [SerializeField]private float botCheckRadius; //地面判定半径
    [SerializeField] private LayerMask groundForCheck; //判定的层级
    [SerializeField] private bool isJumping; //是否在跳跃中

    [Header("长按跳跃参数")]
    [SerializeField] private float holdJumpTime; //长跳持续时间
    [SerializeField] private bool isHoldJumping; //是否在长跳中
     private Coroutine holdJumpCoroutine; //长跳计时器 接收后用于终止
    
    

    [Header("冲刺参数")] 
    [SerializeField] private float dashForce; //冲刺力
    [SerializeField] private float inputFaceDirectionRaw; //面朝方向 正右负左 整数
    [SerializeField] private float dashCoolDown; //冲刺冷却时间
    [SerializeField] private bool dashReady; //冲刺是否冷却完成
    [SerializeField] private bool isDashing; //是否正在冲刺
    [SerializeField] private float DashTime; //冲刺持续时间


    [Header("攻击参数")] 
    [SerializeField] private float attackCoolDown; //攻击间隔
    [SerializeField] private bool attackReady; //攻击是否冷却完成
    [SerializeField] private float attackTime; //一次攻击持续时间
    [SerializeField] private bool isAttacking; //是否正在攻击
    

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>(); //得刚体
        AttackBox = gameObject.GetComponentsInChildren<BoxCollider2D>()[2]; //得攻击碰撞器
        anim = gameObject.GetComponent<Animator>(); //得动画状态机
        AttackBox.enabled = false; //开始时失活碰撞器

        dashReady = true; //开始时可以冲刺
        attackReady = true; //开始时可以攻击

        InputMgr.Instance.SwitchAllButtons(true); //开启输入检测
        EventMgr.Instance.AddEventListener<KeyCode>("KeyIsHeld", PlayerMoveHold); //玩家移动事件监听 长按
        EventMgr.Instance.AddEventListener<KeyCode>("KeyIsPressed", PlayerMovePress); //玩家移动事件监听 按下
        EventMgr.Instance.AddEventListener<KeyCode>("KeyIsReleased", PlayerMoveRelease); //玩家移动事件监听 抬起
        
    }
    
    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(botPos.position, botCheckRadius, groundForCheck); //每帧检测是否触地
        inputFaceDirection = Input.GetAxis("Horizontal");//每帧更新方向
        inputFaceDirectionRaw = Input.GetAxisRaw("Horizontal"); //每帧更新方向（整型）
        
        if (rb.IsSleeping()) //防止刚体休眠
            rb.WakeUp();

        if (rb.velocity.y < 0)
        {
            ChangeAnimationState(fall,isLocked); //掉落直接切换动画
        }
        
        if (currentState == fall && isGrounded) //掉回地面变成idle
        {
            ChangeAnimationState(idle,isLocked);
        }
    }

    


    /// <summary>
    /// 所有的长按操作
    /// </summary>
    /// <param name="key"></param>
    private void PlayerMoveHold(KeyCode key) 
    {
        if (!isDashing) //冲刺时不能左右移动或转向 （冲刺和短按操作不冲突，和长按操作冲突）
        {
            if (isMoving && !isAttacking) //只有移动时 并且不在攻击 才能转向 （攻击时转向会一下a很多东西）
            {
                TurnAround(); //转向
            }
            LeftRightMove(key); //左右移动
            HoldJumpProcess(key);//长按跳跃     
        }
    }

    /// <summary>
    /// 所有短按一次性操作
    /// </summary>
    /// <param name="key"></param>
    private void PlayerMovePress(KeyCode key) 
    {
        PressJump(key);//跳跃
        DashProcess(key); //冲刺
        PlayerAttackProcess(key); //攻击
    }

    /// <summary>
    /// 所有松开一次性操作
    /// </summary>
    /// <param name="key"></param>
    private void PlayerMoveRelease(KeyCode key)
    {
        ReleaseJump(key); //松开长按跳跃
        ReleaseLeftRightMove(key); //松开长按移动
    }

    
    
    
    
    /// <summary>
    ///转向 长按操作
    /// </summary>
    private void TurnAround()
    {
        transform.eulerAngles = inputFaceDirectionRaw >= 0 ? new Vector3(0, 0, 0) : new Vector3(0, 180, 0);
        faceDirectionRaw = (int)inputFaceDirectionRaw;
    }
    /// <summary>
    /// 左右移动 长按操作
    /// </summary>
    private void LeftRightMove(KeyCode key)
    {
        //向左走
        if (key == InputMgr.Instance.KeySet["left"]) //按住左走键
        {
            isMoving = true;
            rb.velocity = new Vector2(inputFaceDirection * speed, rb.velocity.y);
            if (isGrounded) //只有在地上才有移动动画
              ChangeAnimationState(move,isLocked);
        }
        //向右走
        if (key == InputMgr.Instance.KeySet["right"]) //按住右走键
        {
            isMoving = true;
            rb.velocity = new Vector2(inputFaceDirection * speed, rb.velocity.y);
            if (isGrounded) //只有在地上才有移动动画
               ChangeAnimationState(move,isLocked);
        }
    }

    
    
    /// <summary>
    /// 长按跳跃 长按操作
    /// </summary>
    /// <param name="key"></param>
    private void HoldJumpProcess(KeyCode key)
    {
        if (isHoldJumping && key == InputMgr.Instance.KeySet["jump"]) //按住跳跃键并且在长跳状态
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce); //接着按下的小跳继续跳
        }
    }
    
    
    
    
    
    /// <summary>
    /// 短按跳跃 短按操作 倒计时
    /// </summary>
    /// <param name="key"></param>
    private void PressJump(KeyCode key)
    {
        if (isGrounded && key == InputMgr.Instance.KeySet["jump"]) //按了跳跃键 并且在地上
        {
            isJumping = true; //处于跳跃状态
            rb.velocity = new Vector2(rb.velocity.x, jumpForce); //跳
            ChangeAnimationState(jump,isLocked);
        }
        if (isJumping && rb.velocity.y > 2)
        {
            isJumping = false;
            holdJumpCoroutine = TimeMgr.Instance.StartFuncTimer(holdJumpTime, () => { isHoldJumping = true;}, () =>
            {
                isHoldJumping = false;
            }); //开始长跳倒计时
        }
    }
    
    
    /// <summary>
    /// 冲刺 短按操作 计时器
    /// </summary>
    private void DashProcess(KeyCode key)
    {
        if (key == InputMgr.Instance.KeySet["dash"] ) //按下冲刺键
        {
            if (isDashing || dashReady == false) return; //如果正在冲刺或者冲刺在冷却就不能使用
            TimeMgr.Instance.StartFuncTimer(DashTime, Dash, AfterDash); //冲刺开始计时
        }
    }
    /// <summary>
    /// 冲刺逻辑
    /// </summary>
    private void Dash()
    {
        isDashing = true; //进入冲刺状态
        isJumping = false; //冲刺时候不能跳，冲刺完也不能跳
        ChangeAnimationState(dash,isLocked);
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation; //位或 锁z和 y轴
        rb.velocity = Vector2.right * (dashForce * faceDirectionRaw); //往面朝向冲
    }
    /// <summary>
    /// 冲刺后逻辑
    /// </summary>
    private void AfterDash()
    {
        isDashing = false; //退出冲刺状态
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; //只锁z轴 y轴解冻
        rb.velocity = Vector2.zero; //冲刺结束后速度归零 防止速度延续
        if (isGrounded)
            ChangeAnimationState(idle,isLocked);
        else ChangeAnimationState(fall,isLocked);
        //冲刺间冷却计时
        TimeMgr.Instance.StartFuncTimer(dashCoolDown, ()=>{dashReady = false;}, () => { dashReady = true; });
    }

    
    
    /// <summary>
    /// 玩家攻击 短按操作 计时器
    /// </summary>
    private void PlayerAttackProcess(KeyCode key)
    {
        if (key == InputMgr.Instance.KeySet["attack"])
        {
            if (isAttacking || attackReady == false) return;  //如果正在攻击或正在冷却就不能攻击
            TimeMgr.Instance.StartFuncTimer(attackTime, PlayerAttack, AfterPlayerAttack); //攻击开始计时
        }
    }
    /// <summary>
    /// 攻击逻辑
    /// </summary>
    private void PlayerAttack()
    {
        isAttacking = true; //进入攻击状态
        AttackBox.enabled = true; //开启攻击判定
        ChangeAnimationState(attack,isLocked);
        isLocked = true;
    }
    /// <summary>
    /// 攻击后逻辑
    /// </summary>
    private void AfterPlayerAttack()
    {
        isAttacking = false; //退出攻击状态
        AttackBox.enabled = false; //关闭攻击判定
        isLocked = false;
        ChangeAnimationState(idle,isLocked); 
        //攻击间冷却计时
        TimeMgr.Instance.StartFuncTimer(attackCoolDown, ()=>{attackReady = false;}, () => { attackReady = true; });
    }
    


    
    
    /// <summary>
    /// 松开长按跳跃 松键操作
    /// </summary>
    /// <param name="key"></param>
    private void ReleaseJump(KeyCode key)
    {
        if (key == InputMgr.Instance.KeySet["jump"] ) //松开长按跳跃 
        {
            if (isHoldJumping)//并且在长跳跃时
            {
                TimeMgr.Instance.StopTimer(holdJumpCoroutine); //停止协程 阻止计时器继续计时
                isHoldJumping = false; //退出长跳状态
                ChangeAnimationState(fall,isLocked);
            }
        }
    }

    /// <summary>
    /// 松开长按移动 松键操作
    /// </summary>
    /// <param name="key"></param>
    private void ReleaseLeftRightMove(KeyCode key)
    {
        if (key == InputMgr.Instance.KeySet["left"] || key == InputMgr.Instance.KeySet["right"]) //松开移动
        {
            isMoving = false; //退出移动状态
            if (!isDashing) //冲刺时松开不会停止移动
            {
                rb.velocity = new Vector2(0, rb.velocity.y); //松开移动直接停止
                if (isGrounded)
                {
                    ChangeAnimationState(idle, isLocked);
                }
            }
        }
    }
    

    private void ChangeAnimationState(string newState, bool locked) //改变动画状态
    {
        if (currentState == newState) return;
        if (locked) return;
        anim.Play(newState);
        currentState = newState;
    }
}

