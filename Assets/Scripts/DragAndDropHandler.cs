using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;

    [SerializeField] private World world;
    
    private GameInputs gameInputs;

    Vector2 _cursorPosition;
    
    private void Start()
    {
        gameInputs = new GameInputs();
        gameInputs.UI.CursorClick.performed += OnHandleSlotClick;
        gameInputs.Enable();
        
        cursorItemSlot = new ItemSlot(cursorSlot);
        _cursorPosition = new Vector2(Screen.width / 2, Screen.height / 2);
    }

    private void Update()
    {
        if (!world.inUI)
            return;
        
        if (Gamepad.current != null)
        {
            var delta = gameInputs.UI.GamepadMouse.ReadValue<Vector2>();
            _cursorPosition += delta * World.Instance.settings.mouseSensitivity;
            _cursorPosition.x = Mathf.Clamp(_cursorPosition.x, 0, Screen.width);
            _cursorPosition.y = Mathf.Clamp(_cursorPosition.y, 0, Screen.height);
            Mouse.current.WarpCursorPosition(_cursorPosition);

            cursorSlot.transform.position = _cursorPosition;
        }
        else
        {
            cursorSlot.transform.position = Mouse.current.position.ReadValue();
        }
    }
    
    private void OnHandleSlotClick(InputAction.CallbackContext context)
    {
        HandleSlotClick(CheckForSlot());
    }

    private void HandleSlotClick(UIItemSlot clickSlot)
    {
        if (clickSlot == null) return;
        if (!cursorSlot.HasItem && !clickSlot.HasItem) return;

        if (clickSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickSlot.itemSlot.stack);
        }

        if (!cursorSlot.HasItem && clickSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickSlot.itemSlot.TakeAll());
            return;
        }
        
        if (cursorSlot.HasItem && !clickSlot.HasItem)
        {
            clickSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && clickSlot.HasItem)
        {
            if (cursorSlot.itemSlot.stack.id != clickSlot.itemSlot.stack.id)
            {
                var oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                var oldSlot = clickSlot.itemSlot.TakeAll();
                
                clickSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);
            }
        }
    }

    private UIItemSlot CheckForSlot()
    {
        if (m_Raycaster == null) return null;
        m_PointerEventData = new PointerEventData(m_EventSystem)
        {
            position = Gamepad.current != null ? _cursorPosition : Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);
        
        foreach (var result in results.Where(result => result.gameObject.CompareTag("UIItemSlot")))
            return result.gameObject.GetComponent<UIItemSlot>();
        
        cursorSlot.itemSlot.EmptySlot();
        return null;
    }

    private void OnDestroy()
    {
        Destroy(m_Raycaster);
    }
}
