using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using XMLib;
using XMLib.AM;

public enum InputEvents
{
    None = 0b0000,
    Moving = 0b0001,
    Attack = 0b0010,
    Jump = 0b0100,
    Jumping = 0b1000,
}

public static class InputData
{
    public static InputEvents inputEvents { get; set; } = InputEvents.None;
    public static Vector2 axisValue { get; set; } = Vector2.zero;

    public static bool HasEvent(InputEvents e, bool fullMatch = false)
    {
        return fullMatch ? ((inputEvents & e) == inputEvents) : ((inputEvents & e) != 0);
    }

    public static void Clear()
    {
        inputEvents = InputEvents.None;
        axisValue = Vector2.zero;
    }
}

public class GameManager : MonoBehaviour
{
    [SerializeField]
    protected List<ActionMachineController> controllers;

    [SerializeField]
    protected List<TextAsset> configs;
    
#if USE_FIXPOINT
    protected FPPhysics.Fix64 logicTimer = 0;
    protected FPPhysics.Fix64 logicDeltaTime = (FPPhysics.Fix64) (1 / 30f);
#else
    protected float logicTimer = 0f;
    protected const float logicDeltaTime = 1 / 30f;
#endif
   

    #region Input

    public GameInput input;

    #endregion Input

    private void Awake()
    {
        //初始化配置文件加载函数
        ActionMachineHelper.Init(OnActionMachineConfigLoader);
        input = new GameInput();
        input.Enable();

        Physics.autoSimulation = false;
    }

    private MachineConfig OnActionMachineConfigLoader(string configName)
    {
        TextAsset asset = configs.Find(t => string.Compare(t.name, configName) == 0);
        return DataUtility.FromJson<MachineConfig>(asset.text);
    }

    private void OnDestroy()
    {
        input.Disable();
    }

    private void Update()
    {
        UpdateInput();
        LogicUpdate();
    }

    private void UpdateInput()
    {
        var player = input.Player;
        var move = player.Move.ReadValue<Vector2>();
        if (player.Move.phase == InputActionPhase.Started)
        {
            InputData.inputEvents |= InputEvents.Moving;
            InputData.axisValue = move;
        }

        if (player.Attack.triggered)
        {
            InputData.inputEvents |= InputEvents.Attack;
        }

        if (player.Jump.triggered)
        {
            InputData.inputEvents |= InputEvents.Jump;
        }

        if (player.Jump.phase == InputActionPhase.Started)
        {
            InputData.inputEvents |= InputEvents.Jumping;
        }
    }

    private void LogicUpdate()
    {
#if USE_FIXPOINT
        logicTimer += (FPPhysics.Fix64)Time.deltaTime;
#else
        logicTimer += Time.deltaTime;
#endif
        if (logicTimer >= logicDeltaTime)
        {
            logicTimer -= logicDeltaTime;

            RunLogicUpdate(logicDeltaTime);
        }
    }

#if USE_FIXPOINT
    private void RunLogicUpdate(FPPhysics.Fix64 logicDeltaTime)
#else
     private void RunLogicUpdate(float logicDeltaTime)
#endif
    {
        foreach (var item in controllers)
        {
            item.LogicUpdate(logicDeltaTime);
        }

        //更新物理
        Physics.Simulate(logicDeltaTime);

        //清理输入
        InputData.Clear();
    }
}