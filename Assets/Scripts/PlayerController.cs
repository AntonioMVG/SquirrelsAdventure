using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Public variables
    public int speed;
    public int jumpForce;
    public int lives;
    public float levelTime; // In seconds
    public Canvas canvas;
    #endregion

    #region Private variables
    private Rigidbody2D rb;
    private GameObject foot;
    private SpriteRenderer sprite;
    private Animator playerAnimation;
    private GameManager gameManager;
    private EnemyController enemyController;
    private HUDController hud;
    private CollectibleController colController;
    private bool hasJumped;
    private bool sneaking;
    private float initialPosX;
    private float updatedPosX;
    private bool intoTheHell;

    // New mechanic
    private bool isRewinding;
    List<Vector3> positions;
    #endregion

    private void Start()
    {
        GameManager.instance.ResumeGame();
        rb = GetComponent<Rigidbody2D>();
        foot = transform.Find("Foot").gameObject;
        initialPosX = transform.position.x;

        // Get the sprite component of the child sprite object
        sprite = gameObject.transform.Find("player-idle-1").GetComponent<SpriteRenderer>();
        
        // Get the animator controller of the sprite child object
        playerAnimation = gameObject.transform.Find("player-idle-1").GetComponent<Animator>();

        // Get the HUD Controller
        hud = canvas.GetComponent<HUDController>();
        hud.SetLivesTxt(lives);
        hud.SetCollectiblesTxt(GameObject.FindGameObjectsWithTag("Collectibles").Length);
        hud.SetEnemiesTxt(GameObject.FindGameObjectsWithTag("Enemies").Length);

        // New mechanic
        positions = new List<Vector3>();
    }

    private void FixedUpdate()
    {
        // Get the value from -1 to 1 of the horizontal move
        float inputX = Input.GetAxis("Horizontal");

        // Apply physic velocity to the object with the move value * speed, the Y coordenate is the same
        rb.velocity = new Vector2(inputX * speed, rb.velocity.y);

        // Calculate if the time is finnish
        if (levelTime <= 0f)
        {
            //WinLevel(false);
            GameManager.instance.PauseGame();
            hud.SetTimesUpBox();
        }
        else
        {
            levelTime -= Time.deltaTime;
            hud.SetTimeTxt((int)levelTime);
        }

        // New mechanic
        if (isRewinding)
        {
            Rewind();
        }
        else
        {
            Record();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && TouchGround())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            hasJumped = true;
        }

        // Changing the sprite orientation
        if (rb.velocity.x > 0)
        {
            sprite.flipX = false;
        }
        else if (rb.velocity.x < 0)
        {
            sprite.flipX = true;
        }

        // If the player press S or KeyDown, the squirrel crouches
        if((Input.GetKeyDown(KeyCode.S)) || (Input.GetKeyDown(KeyCode.DownArrow)))
        {
            sneaking = true;
        }
        else if ((Input.GetKeyUp(KeyCode.S)) || (Input.GetKeyUp(KeyCode.DownArrow)))
        {
            sneaking = false;
        }

        // Player animations
        PlayerAnimate();

        // Count how many steps the character takes according to its direction
        if ((Input.GetKeyUp(KeyCode.RightArrow)) || (Input.GetKeyUp(KeyCode.LeftArrow)))
        {
            updatedPosX = transform.position.x;
            initialPosX = updatedPosX;
        }

        // When Hell is activated and the F key has been pressed, the game goes back in time
        if (intoTheHell == true && Input.GetKeyDown(KeyCode.F))
        {
            StartRewind();
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            StopRewind();
        }
    }

    private bool TouchGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(foot.transform.position, Vector2.down, 0.2f);

        return hit.collider != null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            hasJumped = false;
        }
    }

    private void PlayerAnimate()
    {
        // Player Jumping
        if (!TouchGround())
        {
            playerAnimation.Play("playerJump");
        }
        // Player Running
        else if (TouchGround() && Input.GetAxisRaw("Horizontal") != 0)
        {
            playerAnimation.Play("playerRun");
        }
        // Player Idle
        else if (TouchGround() && Input.GetAxisRaw("Horizontal") == 0 && !sneaking)
        {
            sneaking = false;
            playerAnimation.Play("playerIdle");
        }
        // Player Crouch
        else if (TouchGround() && Input.GetKeyDown(KeyCode.S))
        {
            sneaking = true;
            playerAnimation.Play("playerCrouch");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Collectibles")
        {
            Destroy(collision.gameObject);
            Invoke(nameof(InfoCollectibles), 0.1f);
            GameManager.instance.Collectibles -= 1;
        }

        if (collision.gameObject.tag == "Enemies")
        {
            Destroy(collision.gameObject);
            Invoke(nameof(InfoEnemies), 0.1f);
            GameManager.instance.Enemies -= 1;
        }

        // The PowerUp increase the player's jump force
        if (collision.gameObject.tag == "PowerUp")
        {
            Destroy(collision.gameObject);
            jumpForce += 4;
        }

        // This is a new mechanic
        if (collision.gameObject.tag == "Heaven")
        {
            intoTheHell = false;
        }
        
        if (collision.gameObject.tag == "Hell")
        {
            intoTheHell = true;
        }
    }

    private void InfoCollectibles()
    {
        int collectiblesNum = GameObject.FindGameObjectsWithTag("Collectibles").Length;
        hud.SetCollectiblesTxt(collectiblesNum);
    }

    private void InfoEnemies()
    {
        int enemiesNum = GameObject.FindGameObjectsWithTag("Enemies").Length;
        hud.SetEnemiesTxt(enemiesNum);
    }

    public void TakeDamage(int damage)
    {
        lives -= damage;
        hud.SetLivesTxt(lives);
        StartCoroutine(Damaged());

        if (lives == 0)
        {
            hud.SetLivesTxt(lives);
            GameManager.instance.PauseGame();
            hud.SetLoseLivesBox();
        }
    }

    IEnumerator Damaged()
    {
        sprite.color = new Color(255, 0, 0, 1);
        yield return new WaitForSeconds(0.5f);
        sprite.color = new Color(255, 255, 255, 1);
    }

    // New mechanic
    private void StartRewind()
    {
        isRewinding = true;
    }

    private void StopRewind()
    {
        isRewinding = false;
    }

    private void Rewind()
    {
        transform.position = positions[0];
        positions.RemoveAt(0);
    }

    private void Record()
    {
        positions.Insert(0, transform.position);
    }
}
