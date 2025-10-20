using UnityEngine;

public class PlayerController_2 : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 7f;

    [Header("Sprint")]
    public KeyCode sprintKey = KeyCode.LeftShift; // tecla configurable en Inspector
    public float sprintMultiplier = 1.8f;     
    public float sprintTiltAngle = 10f; // ángulo de inclinación al correr

    [Header("Salto")]
    public float jumpForce = 4f;
    public float maxJumpTime = 0.3f;
    public float jumpHoldForce = 4f;
    public LayerMask groundLayer;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Dash")]
    public float dashForce = 12f; // Fuerza del dash
    public float dashDuration = 0.15f; // Tiempo que dura el dash
    public float dashCooldown = 0.5f; // Cooldown entre dashes
    private bool canDash = true;
    private bool hasDashedInAir = false;
    private float dashTime;
    public bool isDashing = false;
    private Vector3 dashDirection;
    private Quaternion originalRotation;

    [Header("DashAbajo")]
    private bool hasDashedDownInAir = false;
    public float downDashForce = 20f; // fuerza del dash vertical hacia abajo
    public bool isDownDashing = false;
    private float downDashTime = 0f;
    public float downDashDuration = 0.15f;

    [Header("Parry")]
    public float parryDuration = 0.5f; // Tiempo que dura el parry
    public float parryCooldown = 5f;
    private bool canParry = true;
    public bool isParrying = false;
    private float parryTimeCounter;
    private Quaternion parryRotation;



    [Header("Visual")]
    public Renderer playerRenderer;  // Renderer del objeto
    public Material dashMaterial;    // Material durante dash horizontal
    public Material dashDownMaterial; // Material durante dash vertical hacia abajo
    public Material parryMaterial;    // Material durante parry
    public Material sprintMaterial;   // Material durante sprint
    private Material originalMaterial; // Material base



    private bool isGrounded = true;
    private bool isJumping = false;
    private float jumpTimeCounter;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private Rigidbody rb;
    public Vector3 inputMove;
    private bool facingRight = true;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
        originalRotation = transform.rotation;
        originalMaterial = playerRenderer.material;

    }

    void Update()
    {

        // --- MOVIMIENTO NORMAL ---
        float moveX = Input.GetAxisRaw("Horizontal");
        inputMove = new Vector3(moveX, 0f, 0f);

        // Rotación horizontal normal
        if (!isDashing)
        {
            if (moveX > 0 && !facingRight) Flip();
            else if (moveX < 0 && facingRight) Flip();
        }

        // Input buffer salto
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Coyote time
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // Ejecutar salto
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            isJumping = true;
            isGrounded = false;
            jumpTimeCounter = 0f;
            rb.velocity = new Vector3(rb.velocity.x, 0f, 0f);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpBufferCounter = 0f;
        }

        // Mantener salto
        if (Input.GetKey(KeyCode.Space) && isJumping)
        {
            if (jumpTimeCounter < maxJumpTime)
            {
                rb.AddForce(Vector3.up * jumpHoldForce, ForceMode.Force);
                jumpTimeCounter += Time.deltaTime;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
            isJumping = false;

        // Velocidad de caída ajustada
        if (rb.velocity.y < 0)
            rb.velocity += Vector3.up * Physics.gravity.y * 1.5f * Time.deltaTime;
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
            rb.velocity += Vector3.up * Physics.gravity.y * 0.5f * Time.deltaTime;

        // --- DASH ---
        if (Input.GetKeyDown(KeyCode.J) && canDash)
        {
            if (isGrounded || (!isGrounded && !hasDashedInAir))
            {
                StartDash();
            }
        }

        if (isDashing)
        {
            rb.velocity = dashDirection * dashForce;
            dashTime += Time.deltaTime;
            if (dashTime >= dashDuration)
            {
                EndDash();
            }
        }

        // --- PARRY ---
        if (Input.GetKeyDown(KeyCode.K) && canParry && !isDashing)
        {
            StartParry();
        }

        if (isParrying)
        {
            parryTimeCounter += Time.deltaTime;
            if (parryTimeCounter >= parryDuration)
            {
                EndParry();
            }

            // Mantener al jugador inmóvil durante parry
            rb.velocity = Vector3.zero;
            inputMove = Vector3.zero;
            return; // bloquea cualquier otra acción mientras está en parry
        }

        // --- DASH VERTICAL HACIA ABAJO ---
        if (Input.GetKeyDown(KeyCode.S) && !isGrounded && !isParrying && !isDownDashing && !hasDashedDownInAir)
        {
            StartDownDash();
        }

        if (isDownDashing)
        {
            // Fuerza vertical hacia abajo
            rb.velocity = Vector3.down * downDashForce;

            downDashTime += Time.deltaTime;
            if (downDashTime >= downDashDuration)
            {
                EndDownDash();
            }
        }

    }

    void FixedUpdate()
    {
        if (!isDashing && !isParrying && !isDownDashing)
        {
            float currentSpeed = speed;
            bool isSprinting = isGrounded && Input.GetKey(sprintKey) && inputMove.x != 0;

            // Sprint solo si está en piso y moviéndose
            if (isSprinting)
            {
                currentSpeed *= sprintMultiplier;

                // Cambiar material si está definido
                if (playerRenderer != null && sprintMaterial != null)
                    playerRenderer.material = sprintMaterial;

                // Inclinación hacia la dirección de movimiento
                float tilt = inputMove.x > 0 ? -sprintTiltAngle : sprintTiltAngle;
                transform.rotation = Quaternion.Euler(0, 0, tilt);
            }
            else
            {
                // Restaurar material y rotación original
                if (playerRenderer != null)
                    playerRenderer.material = originalMaterial;

                transform.rotation = originalRotation;
            }

            Vector3 newVelocity = new Vector3(inputMove.x * currentSpeed, rb.velocity.y, 0f);
            rb.velocity = newVelocity;
        }
    }


    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void StartDash()
    {
        isDashing = true;
        dashTime = 0f;
        canDash = false;

        if (playerRenderer != null && dashMaterial != null)
            playerRenderer.material = dashMaterial;

        dashDirection = facingRight ? Vector3.right : Vector3.left;
        if (!isGrounded) hasDashedInAir = true;

        float angle = facingRight ? -45f : 45f;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Invoke(nameof(ResetDashCooldown), dashCooldown);
    }

    private void EndDash()
    {
        isDashing = false;
        transform.rotation = originalRotation;

        if (playerRenderer != null)
            playerRenderer.material = originalMaterial;
    }

    private void ResetDashCooldown()
    {
        canDash = true;
        playerRenderer.material = originalMaterial;

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
                {
                    isGrounded = true;
                    isJumping = false;
                    hasDashedInAir = false; // reset dash aire al tocar piso
                    hasDashedDownInAir = false; // Reset del dash vertical
                    break;
                }
            }
        }
    }


    // --- DASH ABAJO ---
    private void StartDownDash()
    {
        isDownDashing = true;
        downDashTime = 0f;
        hasDashedDownInAir = true;

        rb.velocity = Vector3.zero;

        if (playerRenderer != null && dashDownMaterial != null)
            playerRenderer.material = dashDownMaterial;
    }

    private void EndDownDash()
    {
        isDownDashing = false;

        if (playerRenderer != null)
            playerRenderer.material = originalMaterial;
    }

    // --- PARRY ---
    private void StartParry()
    {
        isParrying = true;
        canParry = false;
        parryTimeCounter = 0f;

        rb.velocity = Vector3.zero;
        rb.useGravity = false;

        if (playerRenderer != null && parryMaterial != null)
            playerRenderer.material = parryMaterial;
    }

    private void EndParry()
    {
        isParrying = false;

        if (playerRenderer != null)
            playerRenderer.material = originalMaterial;

        rb.useGravity = true;
        Invoke(nameof(ResetParryCooldown), parryCooldown);
    }


    private void ResetParryCooldown()
    {
        canParry = true;
    }


}
