using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;
    
    [SerializeField] Transform cam;
    [SerializeField] World world;
    [SerializeField] private Toolbar toolbar;
    [SerializeField] private GameObject butonsPanel;
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.3f;

    public byte orientation;
    
    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform highlightBlock;
    public Transform placeBlock;

    public float checkIncrement = 0.1f;
    public float reach = 8f;
    
    private GameInputs gameInputs;
    private readonly int halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
    
    private void Start()
    {
        SetInputs();
        world.inUI = false;
        cam.rotation = Quaternion.Euler(new Vector3(0, transform.localEulerAngles.y, 0));
    }

    private void SetInputs()
    {
        gameInputs = new GameInputs();
        gameInputs.Player.Move.started += OnMove;
        gameInputs.Player.Move.performed += OnMove;
        gameInputs.Player.Move.canceled += OnMove;
        
        gameInputs.Player.Camera.started += OnCamera;
        gameInputs.Player.Camera.performed += OnCamera;
        gameInputs.Player.Camera.canceled += OnCamera;
        
        gameInputs.Player.Sprint.performed += OnSprint;
        
        gameInputs.Player.Jump.performed += OnJump;
        gameInputs.Player.PlaceVoxel.performed += OnPlaceVoxel;
        gameInputs.Player.DestroyVoxel.performed += OnDestroyVoxel;

        gameInputs.Player.SlotChangeLeft.performed += OnSlotLeft;
        gameInputs.Player.SlotChangeRight.performed += OnSlotRight;

        gameInputs.Player.CameraReset.performed += OnCameraReset;
        
        gameInputs.UI.OpenInventory.performed += OnOpenInventory;

        gameInputs.Main.Quit.performed += OnQuit;

        gameInputs.Enable();
    }
    
    private void FixedUpdate()
    {
        if (!world.IsWorldLoaded) return;
        if (!world.inUI)
        {
            CalclateVelocity();
            if (jumpRequest)
                Jump();
            
            transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);
            cam.Rotate(Vector3.right * -mouseVertical * world.settings.mouseSensitivity);
            transform.Translate(velocity, Space.World);
        }
    }
    
    private void Update()
    {
        if (!world.IsWorldLoaded) return;
        if (!world.inUI)
        {
            if (!gameInputs.Player.enabled) gameInputs.Player.Enable();
            PlaceCursorBlocks();
        }
        else
        {
            if (gameInputs.Player.enabled) gameInputs.Player.Disable();
        }

        var XZDirection = transform.forward;
        XZDirection.y = 0;
        if (Vector3.Angle(XZDirection, Vector3.forward) <= 45)
            orientation = 0;
        else if (Vector3.Angle(XZDirection, Vector3.right) <= 45)
            orientation = 5;
        else if (Vector3.Angle(XZDirection, Vector3.back) <= 45)
            orientation = 1;
        else
            orientation = 4;
    }

    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalclateVelocity()
    {
        // Affect vertical momentum with gravity
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;

        var transform1 = transform;
        // if we're sprinting, use the sprint multiplier
        if (isSprinting)
        {
            velocity = ((transform1.forward * vertical) + (transform1.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        }
        else
        {
            velocity = ((transform1.forward * vertical) + (transform1.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        }

        // Apply vertical momentum (falling/jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
            velocity.z = 0;

        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
            velocity.x = 0;

        if (velocity.y < 0)
            velocity.y = CheckDownSpeed(velocity.y);
        else if (velocity.y > 0)
            velocity.y = CheckUpSpeed(velocity.y);
        WorldEnd();
    }

    private void WorldEnd()
    {
        var position = world.player.transform.position;
        if (position.x - halfWorldSizeInVoxels >= 52)
            velocity.x = -0.01f;
        if (position.x - halfWorldSizeInVoxels <= -52)
            velocity.x = 0.01f;
        if (position.z - halfWorldSizeInVoxels >= 52)
            velocity.z = -0.01f;
        if (position.z - halfWorldSizeInVoxels <= -52)
            velocity.z = 0.01f;
    }

    private float CheckDownSpeed(float downSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
            )
        {
            isGrounded = true;
            return 0;
        }
        {
            isGrounded = false;
            return downSpeed;   
        }
    }

    private void PlaceCursorBlocks()
    {
        var step = checkIncrement;
        var lastPos = new Vector3();

        while (step < reach)
        {
            var pos = cam.position + (cam.forward * step);

            if (world.CheckForVoxel(pos))
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;
                
                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);
                
                return;
            }
            
            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
            step += checkIncrement;
        }
        
        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
    }
    private float CheckUpSpeed(float upSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth))
        )
        {
            return 0;
        }
        {
            return upSpeed;   
        }
    }
    
    private void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = !isSprinting;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        var moveInputValue = context.ReadValue<Vector2>();
        horizontal = moveInputValue.x;
        vertical = moveInputValue.y;
    }

    private void OnSlotRight(InputAction.CallbackContext context)
    {
        toolbar.slotIndex++;
        if (toolbar.slotIndex > toolbar.slots.Length - 1)
            toolbar.slotIndex = 0;
          
        toolbar.highlight.position = toolbar.slots[toolbar.slotIndex].slotIcon.transform.position;
    }
    
    private void OnSlotLeft(InputAction.CallbackContext context)
    {
        toolbar.slotIndex--;
        if (toolbar.slotIndex < 0)
            toolbar.slotIndex = toolbar.slots.Length - 1;
        
        toolbar.highlight.position = toolbar.slots[toolbar.slotIndex].slotIcon.transform.position;
    }
    
    private void OnCamera(InputAction.CallbackContext context)
    {
        var cameraInputValue = context.ReadValue<Vector2>();
        mouseHorizontal = cameraInputValue.x;
        mouseVertical = cameraInputValue.y;
    }
    
    private void OnCameraReset(InputAction.CallbackContext context)
    {
        cam.rotation = Quaternion.Euler(new Vector3(0, transform.localEulerAngles.y, 0));
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
            jumpRequest = true;
    }

    private void OnPlaceVoxel(InputAction.CallbackContext context)
    {
        if (highlightBlock.gameObject.activeSelf)
            if (toolbar.slots[toolbar.slotIndex].HasItem)
            {
                var position = placeBlock.position;
                world.GetChunkFromVector3(position).EditVoxel(position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                // toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
            } 
    }
    
    private void OnDestroyVoxel(InputAction.CallbackContext context)
    {
        if (highlightBlock.gameObject.activeSelf && highlightBlock.position.y > 0)
            world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
    }

    private void OnOpenInventory(InputAction.CallbackContext context)
    {
        world.inUI = !world.inUI;
        butonsPanel.SetActive(world.inUI);
    }
    
    private void OnQuit(InputAction.CallbackContext context)
    {
        // Application.Quit();
    }

    public bool front
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))
            )
                return true;
            return false;
        }
    }
    
    public bool back
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))
            )
                return true;
            return false;
        }
    }
    
    public bool left
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))
            )
                return true;
            return false;
        }
    }
    
    public bool right
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))
            )
                return true;
            return false;
        }
    }

    private void OnDestroy()
    {
        gameInputs.Dispose();
    }
}
