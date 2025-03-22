using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal movement speed")]
    public float moveSpeed = 5f;
    [Tooltip("Force applied when jumping")]
    public float jumpForce = 5f;
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer;

    [Header("Ability Selection")]
    [Tooltip("UI object for ability selection")]
    public GameObject habSelector;
    [Tooltip("Prefab for projectile spawning")]
    public GameObject projectilePrefab;
    [Tooltip("Speed of fired projectiles")]
    public float projectileSpeed = 10f;
    [Tooltip("Point where projectiles spawn")]
    public Transform firePoint;

    [Header("Dismemberment")]
    [Tooltip("Head object for dismemberment")]
    public GameObject headObject;
    [Tooltip("Body object for dismemberment")]
    public GameObject bodyObject;
    [Tooltip("Is the player currently dismembered?")]
    public bool isDismembered = false;

    [Header("Components")]
    [Tooltip("Player's Rigidbody2D component")]
    public Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    [Tooltip("Player's BoxCollider2D component")]
    public BoxCollider2D boxCollider;
    private Animator animator;

    [Header("State Variables")]
    [Tooltip("Is the player touching the ground?")]
    public bool isGrounded;
    private float gravityScale;
    [Tooltip("Is gravity in normal direction?")]
    public bool isGravityNormal = true;
    private float lastGravityChange;
    private float gravityChangeDelay = 0.5f;
    private bool hasTouchedGround = false;
    private bool canChangeGravityInAir = false; // Permitir cambio de gravedad en el aire una vez
    private bool isShooting = false;
    private float facingDirection = 1f;
    [Tooltip("Is the player in ability selection mode?")]
    public bool isSelectingMode = false;
    private bool isMovementLocked = false;
    private bool hasSelectedWithX = false;
    private bool justExitedSelection = false;

    [Header("Dialogue System")]
    [Tooltip("Currently active dialogue system")]
    public object activeDialogueSystem;

    [Header("Audio")]
    private AudioSource audioSource;
    [Tooltip("Sound played when jumping")]
    public AudioClip jumpSound;

    [Header("Abilities")]
    [Tooltip("Can the player change gravity?")]
    public bool canChangeGravity = false;
    [Tooltip("Can the player shoot projectiles?")]
    public bool canShoot = false;
    [Tooltip("Can the player dismember?")]
    public bool canDismember = false;

    [Header("UI Elements")]
    [Tooltip("Sprite shown for locked abilities")]
    public Sprite lockedLogo;
    [SerializeField]
    [Tooltip("Reference to CoinControllerUI")]
    private CoinControllerUI coinControllerUI;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        gravityScale = rb.gravityScale;
        lastGravityChange = -gravityChangeDelay;

        if (habSelector != null) habSelector.SetActive(false);
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), 1f);
        Time.timeScale = 1f;

        if (headObject != null) headObject.SetActive(false);
        if (bodyObject != null) bodyObject.SetActive(false);

        if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (coinControllerUI == null)
        {
            Debug.LogError("CoinControllerUI no está asignado en PlayerMovement.");
        }
    }

    void Update()
    {
        if (activeDialogueSystem != null)
        {
            if (activeDialogueSystem is DialogueSystem dialogue && dialogue.IsDialogueActive) return;
            else if (activeDialogueSystem is ItemMessage itemMessage && itemMessage.IsDialogueActive) return;
            activeDialogueSystem = null;
        }

        if (isMovementLocked || isDismembered)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            animator.SetFloat("Speed", 0f);
            if (isDismembered && Input.GetKeyDown(KeyCode.Z))
            {
                ReturnToNormal();
            }
            return;
        }

        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        float adjustedVerticalSpeed = isGravityNormal ? rb.velocity.y : -rb.velocity.y;
        animator.SetFloat("VerticalSpeed", adjustedVerticalSpeed);

        bool hasAnyAbility = canChangeGravity || canShoot || canDismember;
        if (Input.GetKey(KeyCode.X) && hasAnyAbility && !justExitedSelection)
        {
            if (!isSelectingMode)
            {
                if (habSelector != null) habSelector.SetActive(true);
                Time.timeScale = 0f;
                isSelectingMode = true;
                UpdateHabSelectorUI();
            }
        }
        else
        {
            if (habSelector != null && !isSelectingMode) habSelector.SetActive(false);
            Time.timeScale = 1f;
            isSelectingMode = false;
            hasSelectedWithX = false;

            if (!Input.GetKey(KeyCode.X)) justExitedSelection = false;

            float moveInput = 0f;
            if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

            if (moveInput != 0)
            {
                facingDirection = Mathf.Sign(moveInput);
                transform.localScale = new Vector3(facingDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
            {
                float jumpDirection = isGravityNormal ? 1f : -1f;
                rb.velocity = new Vector2(rb.velocity.x, jumpForce * jumpDirection);
                PlayJumpSound();
            }

            // Cambio de gravedad (en el suelo o en el aire una vez)
            if (Input.GetKeyDown(KeyCode.Z) && !isSelectingMode && canChangeGravity && Time.time >= lastGravityChange + gravityChangeDelay)
            {
                if (isGrounded || canChangeGravityInAir)
                {
                    ChangeGravity();
                    if (!isGrounded) canChangeGravityInAir = false; // Desactivar si se usa en el aire
                }
            }

            // Disparar si está en modo shooting
            if (Input.GetKeyDown(KeyCode.Z) && !isSelectingMode && isShooting && canShoot)
            {
                FireProjectile();
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            float dot = Vector2.Dot(normal, isGravityNormal ? Vector2.up : Vector2.down);
            if (dot > 0.5f)
            {
                isGrounded = true;
                hasTouchedGround = true;
                canChangeGravityInAir = true; // Resetear al tocar el suelo
                Debug.Log($"OnCollisionEnter2D: isGrounded={isGrounded}, VerticalSpeed={animator.GetFloat("VerticalSpeed")}, Speed={animator.GetFloat("Speed")}, Time.timeScale={Time.timeScale}");
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;
        }
    }

    private void PlayJumpSound()
    {
        if (jumpSound != null && audioSource != null)
        {
            audioSource.pitch = 1.5f;
            audioSource.PlayOneShot(jumpSound);
        }
    }

    private void UpdateHabSelectorUI()
    {
        if (habSelector == null) return;
        HabSelector habScript = habSelector.GetComponent<HabSelector>();
        if (habScript != null)
        {
            habScript.UpdateUI(canChangeGravity, canShoot, canDismember);
        }
    }

    public void UnlockAbility(string abilityName)
    {
        switch (abilityName.ToLower())
        {
            case "gravity": canChangeGravity = true; break;
            case "shoot": canShoot = true; break;
            case "dismember": canDismember = true; break;
            default: Debug.LogWarning("Habilidad desconocida: " + abilityName); break;
        }
        UpdateHabSelectorUI();
    }

    public void ChangeGravity()
    {
        rb.gravityScale = -rb.gravityScale;
        isGravityNormal = !isGravityNormal;
        Vector3 center = boxCollider.bounds.center;
        transform.RotateAround(center, Vector3.forward, 180f);
        transform.RotateAround(center, Vector3.up, 180f);
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        if (isGrounded && Mathf.Abs(rb.velocity.x) < 0.1f)
        {
            animator.SetBool("IsGrounded", true);
            animator.SetFloat("VerticalSpeed", 0f);
        }
        lastGravityChange = Time.time; // Actualizar el tiempo del cambio
    }

    void FireProjectile()
    {
        if (projectilePrefab != null && firePoint != null && ProjectilePool.Instance.CanShoot())
        {
            GameObject projectile = ProjectilePool.Instance.GetProjectile();
            if (projectile != null)
            {
                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = Quaternion.identity;
                projectile.SetActive(true);
                Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
                projectileRb.gravityScale = 0f;
                Vector2 shootDirection = transform.right * facingDirection;
                projectileRb.velocity = shootDirection.normalized * projectileSpeed;
            }
        }
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    public void DismemberHead()
    {
        if (!isGrounded || !canDismember) return;

        headObject.tag = "Player";
        if (headObject != null && bodyObject != null)
        {
            isDismembered = true;
            spriteRenderer.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;
            rb.velocity = Vector2.zero;
            if (habSelector != null) habSelector.SetActive(false);
            Time.timeScale = 1f;
            isSelectingMode = false;
            bodyObject.SetActive(true);
            headObject.SetActive(true);
            bodyObject.transform.position = transform.position;
            bodyObject.transform.rotation = transform.rotation;
            bodyObject.transform.localScale = transform.localScale;
            Vector3 headOffset = new Vector3(0f, boxCollider.size.y * 0.5f, 0f);
            headObject.transform.position = transform.position + (isGravityNormal ? headOffset : -headOffset);
            headObject.transform.localScale = transform.localScale;
            Rigidbody2D bodyRb = bodyObject.GetComponent<Rigidbody2D>();
            if (bodyRb != null)
            {
                bodyRb.bodyType = RigidbodyType2D.Static;
                bodyRb.velocity = Vector2.zero;
            }
            Rigidbody2D headRb = headObject.GetComponent<Rigidbody2D>();
            if (headRb != null)
            {
                headRb.bodyType = RigidbodyType2D.Dynamic;
                headRb.gravityScale = 1f;
                headRb.velocity = Vector2.zero;
            }
        }
    }

    private void ReturnToNormal()
    {
        if (headObject != null && bodyObject != null)
        {
            isDismembered = false;
            spriteRenderer.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;
            headObject.SetActive(false);
            bodyObject.SetActive(false);
            transform.position = bodyObject.transform.position;
            spriteRenderer.color = Color.white;
            isShooting = false;
            transform.localScale = bodyObject.transform.localScale;
            Time.timeScale = 1f;
        }
    }

    public void RecomposePlayer()
    {
        if (isDismembered) ReturnToNormal();
    }

    public void SetDialogueActive(object dialogue)
    {
        activeDialogueSystem = dialogue;
        if (dialogue == null && animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalSpeed", 0f);
            animator.SetFloat("Speed", 0f);
            Debug.Log("Dialogue Ended: Forcing Animator to Idle state");
        }
    }

    public bool IsGrounded() => isGrounded;
    public bool IsGravityNormal() => isGravityNormal;
    public void SetMovementLocked(bool locked) => isMovementLocked = locked;
    public bool IsXPressed() => Input.GetKey(KeyCode.X);
    public void SetShootingMode(bool shooting) => isShooting = shooting;

    public void ExitSelectingMode()
    {
        if (habSelector != null) habSelector.SetActive(false);
        Time.timeScale = 1f;
        isSelectingMode = false;
        hasSelectedWithX = false;
        justExitedSelection = true;
    }
}