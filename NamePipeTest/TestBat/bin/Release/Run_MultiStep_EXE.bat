@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title 多阶段EXE调用（终极稳定版）

:: ===================== 基础配置 =====================
set "EXE_PATH=%~dp0TestBat.exe"
set "BT_DONGLE_PORT=COM7"
set "DEFAULT_PRODUCT=PB330"
set "SN=0000111"
set "BT_Address=11:22:33"
set "FINAL_EXIT_CODE=0"
set "TEMP_LOG_PREFIX=%~dp0exe_output_"

:: ===================== 初始化变量 =====================
set "CURRENT_STEP=0"
set "TOTAL_STEPS=6"
set "CONTINUE_NEXT=1"
set "ALL_STEPS_PASSED=1"

:: ===================== 处理命令行参数：极简逻辑 =====================
if not "%~1"=="" (
    echo 检测到命令行参数，更新第0阶段参数...
    
    :: 手动提取port和product（如果有），用于后续阶段
    for %%a in (%*) do (
        if "!PORT_TMP!"=="1" set "BT_DONGLE_PORT=%%a"& set "PORT_TMP=0"
        if "!PROD_TMP!"=="1" set "DEFAULT_PRODUCT=%%a"& set "PROD_TMP=0"
        if "!SN_TMP!"=="1" set "SN=%%a"& set "SN_TMP=0"
        if /i "%%a"=="--port" set "PORT_TMP=1"
        if /i "%%a"=="-port" set "PORT_TMP=1"
        if /i "%%a"=="--product" set "PROD_TMP=1"
        if /i "%%a"=="-product" set "PROD_TMP=1"
        if /i "%%a"=="--sn" set "SN_TMP=1"
        if /i "%%a"=="-sn" set "SN_TMP=1"
    )
    echo [更新] 端口：!BT_DONGLE_PORT!，产品：!DEFAULT_PRODUCT!
    
    set "CALL_PARAMS_0=Reset_BT_Dongle|uart --port !BT_DONGLE_PORT! --baudrate 921600 --databits 8 --stopbits 1 --command ^"^>RST^" --format rn --timeout 7000 --expect IDLE"
    set "CALL_PARAMS_1=Get BT Address|sfc --product !DEFAULT_PRODUCT! --type GET_BarcodePartSN_By_MainSN --parameters !SN!;!DEFAULT_PRODUCT!_MAC --displaydata"
    set "CALL_PARAMS_2=BT_SPPConnect|uart --port !BT_DONGLE_PORT! --baudrate 921600 --databits 8 --stopbits 1 --command ^"^>SPP_CONN=!BT_Address!^" --format rn --timeout 10000 --expect SPP_CONNECT"
    set "CALL_PARAMS_3=TestMode|uart --port !BT_DONGLE_PORT! --baudrate 921600 --databits 8 --stopbits 1 --command ^"TL_ATS_IN^" --format rn --timeout 10000 --expect success"
    set "CALL_PARAMS_4=AuxinMode|uart --port !BT_DONGLE_PORT! --baudrate 921600 --databits 8 --stopbits 1 --command ^"TL_SC_AUX^" --format rn --timeout 10000 --expect success"
    set "CALL_PARAMS_5=DUT_VolumeMax|uart --port !BT_DONGLE_PORT! --baudrate 921600 --databits 8 --stopbits 1 --command ^"TL_SET_VOL=32^" --format rn --timeout 10000 --expect success"
)

:: ===================== 清理旧文件 =====================
for /f %%f in ('dir /b "%TEMP_LOG_PREFIX%*.tmp" 2^>nul') do del /f /q "%%f" >nul 2>&1

:: ===================== 检查EXE =====================
if not exist "%EXE_PATH%" (
    echo [错误] EXE文件不存在！
    pause
    exit /b 99
)

:: ===================== 核心循环：执行阶段 =====================
:EXECUTE_STEP
if !CURRENT_STEP! geq !TOTAL_STEPS! goto :END_LOOP
if !CONTINUE_NEXT! equ 0 goto :END_LOOP

:: 动态获取当前阶段的参数
for /f "tokens=1,2 delims=|" %%a in ("!CALL_PARAMS_%CURRENT_STEP%!") do (
    set "STEP_DESC=%%a"
    set "STEP_PARAMS=%%b"
)

:: 生成临时日志
set "TEMP_LOG=%TEMP_LOG_PREFIX%!CURRENT_STEP!_!random!.tmp"

:: ===================== 执行调用 =====================
echo.
echo ======================================================
echo 【第!CURRENT_STEP!阶段调用 - !STEP_DESC!】
echo ======================================================
echo 调用参数：!STEP_PARAMS!

:: 执行EXE
"%EXE_PATH%" !STEP_PARAMS! > "!TEMP_LOG!" 2>&1
type "!TEMP_LOG!"
set "STEP_EXIT_CODE=%errorlevel%"

:: 提取结果
set "STEP_RESULT="
for /f "delims=" %%a in (!TEMP_LOG!) do set "STEP_RESULT=%%a"
if not defined STEP_RESULT set "STEP_RESULT=fail"
for /f "tokens=* delims= " %%a in ("!STEP_RESULT!") do set "STEP_RESULT=%%a"

:: 结果判断
echo.
echo ======================================================
echo 【第!CURRENT_STEP!阶段执行结果】
echo ======================================================
echo EXE最后一行输出结果：!STEP_RESULT!
echo EXE退出码：!STEP_EXIT_CODE!

if !CURRENT_STEP! equ 1 (
    :: Get BT Address阶段：判空
    if "!STEP_RESULT!" == "" (
        echo Get BT Address阶段结果为空，终止后续调用
        set "CONTINUE_NEXT=0"
        set "ALL_STEPS_PASSED=0"
    ) else if /i "!STEP_RESULT!" equ "fail" (
        echo Get BT Address阶段结果为fail，终止后续调用
        set "CONTINUE_NEXT=0"
        set "ALL_STEPS_PASSED=0"
    ) else (
        echo Get BT Address阶段结果有效，赋值BT_Address=!STEP_RESULT!
        set "BT_Address=!STEP_RESULT!"
        :: 在获取到新的BT_Address后，重新设置CALL_PARAMS_2
        :: 重新设置BT_SPPConnect阶段参数...
        set "CALL_PARAMS_2=BT_SPPConnect|uart --port !BT_DONGLE_PORT! --baudrate 921600 --databits 8 --stopbits 1 --command ^"^>SPP_CONN=!BT_Address!^" --format rn --timeout 10000 --expect SPP_CONNECT"
        set "CONTINUE_NEXT=1"
    )
) else (
    :: 其他阶段：判successful
    if /i "!STEP_RESULT!" equ "successful" (
        echo 阶段结果符合预期，继续下一阶段
        set "CONTINUE_NEXT=1"
    ) else (
        echo 阶段结果不符合预期，终止后续调用
        set "CONTINUE_NEXT=0"
        set "ALL_STEPS_PASSED=0"
    )
)
echo ======================================================

:: 清理
if exist "!TEMP_LOG!" del /f /q "!TEMP_LOG!" >nul 2>&1

:: 下一阶段
set /a CURRENT_STEP+=1
goto :EXECUTE_STEP

:END_LOOP
echo.
echo ======================================================
echo 【所有阶段执行完成】
echo ======================================================
echo 总阶段数：!TOTAL_STEPS!
echo 执行到的阶段：!STEP_DESC!阶段
if !ALL_STEPS_PASSED! equ 1 (
    echo 最终总结：PASS
    set "FINAL_EXIT_CODE=PASS"
) else (
    echo 最终总结：FAIL
    set "FINAL_EXIT_CODE=FAIL"
)
echo !FINAL_EXIT_CODE!

endlocal
exit /b !FINAL_EXIT_CODE!