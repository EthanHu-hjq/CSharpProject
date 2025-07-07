# Toucan User Guide

[TOC]

## Toucan V2用户手册

### 概要

#### 目的

Toucan V2是在Toucan V1基础上开发的一套测试执行程序，以实现

1. 统一的术语定义，便于内外部相关人员沟通交流

2. 统一的测试交互逻辑，进一步员工培训成本

3. 进一步打通声学与电子测试间的差别

#### 组件

Toucan V2程序包含三部分

1. EXE前端，基于WPF开发，承载UI交互相关

2. ToucanCore，测试业务逻辑，承载测试相关业务交互实现，以及抽象的测试引擎定义

3. 测试引擎插件，对接不同测试需求，实现测试逻辑到第三方测试软件的驱动与控制

##### 依赖：

1. netfx 4.8 (Window7 可能需要单独下载安装)

2. TestStand Runtime Engine. (**Note:** 请按需安装x64/x86版本，否则可能需要手动switch其Runtime engine版本)

##### 测试引擎

目前支持的测试引擎包括

1. Teststand。支持从TestStand 2017至当前（2023）版本的TestStand程序测试

2. Audio Precious。自适应自持AP5，6.1, 7, 8, 9 （底层API存在不兼容）。

3. TestStand Ultra for A2B。支持超多槽位DUT进行测试，基于TestStand引擎，以TestStand超多槽位存在资源占用多，响应慢的问题

4. Teststand + Audio Precious。支持TestStand + AP测试混合模式，实现简单功能测试与声学混合测试从而减低生产成本的目的

## 操作

例程：参照Toolbox --> PTE -> Test -> Test -> Example

### 新建项目

原则上，Toucan开发的期望ATE在最小测试系统下完成测试开发调试后，直接导入到Toucan中，通过Toucan的外围配置，快速可以实现一套符合公司内部标准要求，以及便于生产调试维护的测试系统。

即期望测试程序在脱离Toucan后也可以实现测试的基本功能。

通用要求

- 尽量减少需要操作员确认的对话框 （提高自动化）

- 尽量不依赖工程目录外的文件，特别是绝对路径（基础环境不含）（提高内聚性）

- 尽量将非测试所需文件移出工程目录（TestStand使用Deploy进行打包）（提高下载解压的效率）

- 尽量将开放维护修改的文件放入子文件夹中，例如./Config (便于打包和分发)

- 区分Fail（Defect）与Error，将不认为是产品本身的问题，设置为Error，而不是Fail，便于做不良分析，以及内部改善
  
  - 后续将会把Error的信息上传至SFCs（不触发维修和工艺流程变更），并通过SFCs的异常报表分析汇总生产异常信息，协助改善程序、治具，以及产品的稳定性

建议要求

- 测试程序要生成Log，生产问题优先通过log去分析复现现场，其次才是现场调试（便于现场快速解决问题，以及实现远程分析处理。减少对Development License的需求）

- TestStand程序，尽量避免在步骤中包含复杂的PostExpression，而是拆分出来，便于工作交接

流程要求：

- 测试信息要与对应Leader核对，不要使用错误的配置信息进行测试

- 试产阶段，要回顾测试数据
  
  - 测试程序是否上传至Toolbox
  
  - 测试数据项目是否一致
  
  - 测试报告是否有上传至服务器 （服务器链接可以在打开的工程中，Toucan右上角快速打开）

#### 测试引擎

##### Audio Precision

在Engineer权限下，用Toucan打开对应的APx工程文件，根据提示，保存即可

###### 脚本要求

- 主Sequnce名称不能是限定词Reference，Verification，建议使用DUT

- 涉及校准，校样的测试，其SignalPathSetup需要点选为 __Checked__，否则将会被忽视

###### 内部变量

APx内置变量包括如下，数据类型均为字符串

TODO: 抓取上传SFCs

- Customer

- Product

- Station

- IsSFC：MES/SFC开关，开为1，关为0

- SN：产品条码

- SlotIndex: 以0开始

- RefBase: REF的文件基础路径，调试时可以手工指定以加载特定的文件

- VerBase: VER的文件基础路径，调试时可以手工指定以加载特定的文件

##### Teststand

在Engineer权限下，用Toucan打开对应的Sequence工程文件，根据提示，保存即可

###### 脚本要求

- Subsequnce保留名称Reference，Verification用作支持REF和VER

###### 内部变量

TestStand的 Toucan内置变量均位于Locals.UUT.AdditionalData下，可以在实时调试界面的PreUUTLoop, PreUUT和PostUUT中的Parameter.UUT.AdditionalData下查看

其变量包括

- GuiVersion: 执行脚本的Toucan版本

- SpecVersion：脚本的规格版本

- IsSFC：MES/SFC开关

- SFCs_ExtColumn：附加的提交至SFC的列名，建议在PreUUTLoop中定义

- SFCs_ExtValue，附加的提交至SFC的数据，建议在Main Sequence的CleanUp中更新，需要与SFCs_ExtColumn对齐

- SFCs_BarcodePart：内部SFC要求的BarcodePart，需要符合SFC团队对BarcodePart的格式要求，以（分号分割，名称@数据，待确认）

- ExtraDefectCode：外部不良编号，优先级较脚本自带要高

- ExtraDefectName：外部不良名称，与ExtraDefectCode对应

- Customer：客户名称

- Product：机种名称

- Station：工站名称

- Attach，附加字段，会体现在最后的测试报告名称中，比如标记金样，REF, VER的测试记录等，常在LVHM中用以标记Pallet编号

- NAS_ExtFile：附加保存文件，文件完整路径，多个文件路径以封号分割

_当前版本暂未开通Teststand的REF和VER_

##### Audio Precision + TestStand

在完成AP和Teststand的独立调试后，需新增一后缀为tsap的文件为主入口文件，其内容分别为TestStand和AP的工程文件名称，文件名以回车换行分割。

程序以TestStand程序为主入口，APx的测试夹杂在Teststand的执行过程中，因此，APx执行的一些前动作需要在Teststand中配置好

测试报告会分别上传至同一目录下，AP的为xlsx格式，Teststand的为xml格式。UI显示会将两部分数据进行合并处理

###### 脚本要求

同TestStand与Audio Precious

- TestStand中在需要执行APx的位置，增加表达式，表达式内容参照下述。Runstate.Thread.PostUIMessageEx(UIMsg_UserMessageBase + 200, RunState.TestSockets.MyIndex, "APx Call", ThisContext, True)

- 表达式只能调用1次，调用多次以最后的那次为准

###### 内部变量

同TestStand与Audio Precious

- ApVarSyncUp: 同步Ap于TestStand之间的变量。在调用AP时，将TestStand中的值写入AP，在AP执行完时，从AP将数据读回到TestStand。_注：只支持字符串格式的_

- ApResult：AP执行完后，返回AP执行的结论，字符串类型。Passed、Failed、Error

注意：自定义扫码时，空条码不应当往下测试，因为APEngine中对空条码进行忽略，即在TestStand切入到AP测试时，会因为AP忽略了该测试，而导致不会返回至TestStand而卡住，只能关闭程序重新打开

##### Teststand A2B (Ultra)

###### 脚本要求

1. 在工程目录新建一个后缀为tsab的入口文件，里面包含TestStand Sequence的名称以及对应的json文件

###### 内部变量

##### SoundCheck (TODO)

#### 工程设置

注意，所有设置均需在解锁状态下才可以更改

##### 工站设置

配置工站的客户，项目，机种，工站等相关信息（以PTE内部标准名称为准，同Toolbox），以及相关的测试配置

![Setting](./src/Setting.png)

##### MES设置

配置测试站的生产管控相关信息，目前支持TYM，PRIMAX，PRIMAX TH

![Setting](./src/Setting_Mes.png)

##### 校准/校样/验证

###### 校准 Calibration

校准是一套争对测量系统的动作，其信源来自于标定的设备或仪器，与被测物无关。原则上测试链路发生了改变，或者测试系统过了可信周期，均需要对测试系统进行校准。

- 例子：声学测试的MIC Sensitivity

![HardwareCalibration](./src/HardwareCalibration.png)

__注：设备校准过程，会修改当前AP工程的SignalPath下的设置（Analog/Acoustic、Balance\Unbalance）__

特别的，针对特定工程，可以实施工程校准，即校准数据与测试工程有绑定关系。工程校准数据只适配于对应工程（客户，机种，工站）。

- 例子：喇叭阻抗测试中，不同机种，适配的线程的阻抗会有差别，额阻抗参数不能只看设备侧这一部分，要加上对应治具中的线材综合测量。

![ProjectCalibration](./src/ProjectCalibration.png)

注：目前只支持AP相关校准。

注：当设备校准于工程校准同时存在时，会先加载设备校准数据，再加载工程校准的数据

###### 校样 Reference

校样，是在不便于直接获取产品的某些性能参数，或者是这些性能参数是以样品进行标定的情况下，用金样获取原始测试数据的过程。

- 例子：声学测试，RF测试的Pathloss

###### 验证 Verification

验证是用以保证测试系统是可信的，避免测试系统随时间推移，产生的变差导致测试数据不可信。一般的做法是使用金样，回测测试数据，测试数据在初始数据的一个偏差规格内。这个规格应当是要比较严格的规格，用以警示生产端做Reference

__讨论：按照这个目的，VER应当是比较的原始采集数据，而不增加Reference的修正，以判断样品或测试系统的变差是否已经恶化__

#### 硬件设置

##### 测试触发

默认情况下，完成扫码之后，即会开始触发测试。但是实际的操作过程中，此时DUT往往还在操作者手中，需要等测试条件具备之后才开始测试。测试触发则是用来实现检测测试条件是否具备的环节。__触发测试，可以有效提升操作员的效率__

目前提供按键触发，夹具触发

带支持外部触发，AP触发（有需求的情况下再实现）

案例：旋转门，门关到位触发测试

##### 夹具控制

测试过程中，常见的需要将DUT紧密的压合在治具内保证良好的接触，需要控制治具进行开合，以及内部托盘的进出等，

1. Auto DUT In：勾选后，测试开始之后，将自动移入载板，然后控制夹具关门。比如。在自动线中，检测到读码器（自动条码枪，RFID）读到SN，执行相应动作。__谨慎使用该项__

2. Auto DUT Out: 勾选后，测试完成之后，将自动驱动夹具执行开门，移出载板的动作

###### 夹具自定义

对于同型号存在不同的软硬件版本的可能，驱动程序需提供IO定制界面，以适配不同连接需求。其中这些定制配置为跟随测试电脑，即在存在这种差异的情况下，__更换电脑时，请额外注意其硬件配置是否会造成以为的损害__

- 案例：旋转门

##### 路由设置

测试过程中，经常涉及到一些上电，开关，升至与多通道的硬件复用等路由切换设置。部门内提供的继电器板，以及测试箱子可能附带了一些集成继电器可以实现此类功能，Toucan现在将这些功能剥离出来，提供给开发工程师在程序内部集成路由外的一个选项

- 案例：继电器板，ME101，控制产品上电与掉电

#### Hooks

在测试中，有一些动作可能不便于在测试脚本中集成，例如当前Toucan或测试引擎不支持，或者需要有其他人协同提供工具，或者为提供更好的可编辑性，我们需要在测试脚本外执行调用一些工具或过程。Toucan提供了一系列的钩子（Hooks）以实现这一功能

- 案例：BB8项目中，需要在每次启动测试软件时，控制步进电机回复到0点，避免不正确的位置导致撞件

__注意：Hooks目前只调用，不判断执行是否正常。即存在被调用的指令没正常执行，但测试流程继续往下跑的情况，比如实际测试环境于调试环境不一样导致命令却依赖__

#### 注入变量

在多槽位测试过程中，对不同槽位可能需要赋予不同的变量值，比如通道资源复用（COM口，采集卡测试通道，继电器通道），以便在测试过程中灵活的对其操作。为此，Toucan提供了槽位变量设置的功能。其会在每次测试前，将对应槽位变量写入到测试引擎内（仅支持AP引擎，TestStand有自己槽位有自带的数据空间， 暂不支持）。

Toucan会根据工程设置中槽位数据自动更新列编号。注入变量会在PreUUT写入测试执行内

![VariableTable](./src/VariableTable.png)

```csv
Name,Type,IsMes,2
VarName,,False,asd,dsa
ddd,,True,444,aaa
ddd,,False,222,333
```

注：目前只接受string类型数据，不能设置数据类型

- 案例：旋转门中，A面要使用COM1，B面要使用COM2进行通讯

##### 金样管理

在校准校样环节，可能需要限定只有受控的金样才可以操作，限定的金样条码可以在这里进行维护。若条码清单为空，则表示不进行限

![GoldenSample](./src/GoldenSample.png)

#### 其他平台迁移

##### APGUI项目

APGUI目前的Config未实现自动解析，同时其REF和VER的实现机制变化，暂时未实现自动打开APGUI的工程

主要修改点

1. 绝对路径依赖：APGUI的工程大多会使用绝对路径C:\ProgramData\Temp\Acoustic进行配置命令行以及REF、VER测试数据

##### Toucan V1项目

Toucan V1R1的项目可以直接重指向到Toucan V2使用

### 更改保存

TestStand不支持直接编辑

### 开发调试

#### 引擎UI

#### 界面UI

#### 附加Tag

调试验证阶段，可能需要在测试报告中增加一些标记，比如DOE，新FW验证之类的，便于后期回溯

#### 堆叠显示

调试验证阶段，可能需要实时查看一下当前批次测试数据的分布信息，以便于实时评估产品变差，更新规格或调整治具。

![AttachResult](./src/AttachResult.png)

#### 相关设置

##### 工程设置

##### 硬件设置

###### 

##### 校准设置

##### 显示设置

#### 工具相关

##### 规格合并工具

有时候，需要修改测试规格，比如增删测试项，调整位置等，又不想原有的不良代码被刷新，可以使用规格合并工具对测试规格进行合并操作。

其将比对新旧测试规格，同名的测试规格的不良代码将被保留，不同名称的测试项目，将在原不良代码最高数字的下一个百位另起不良代码开始编号

注：测试项改名对应的为删除旧测试项，新增改名的项目

![SpecMerge](./src/SpecMerge.png)

##### MES调试工具

![MesHelper](./src/MesHelper.png)

##### 网络分析工具

在有些情况下，生产测试发现报告不能上传，SFCs被Offline等网络相关的故障，可以菜单 --> 帮助 --> 网络助手，查看当前的网络设置，并参照指引确认是否存在设置上的问题。

![NetworkHelper](./src/NetworkHelper.png)

当故障无法排除时，可以点击菜单 --> 帮助 --> 自检，程序将对收集环境相关信息，请提供给Toucan维护人员，协助分析可能的原因

![SelfTest](./src/SelfTest.png) 

##### 驱动调试工具

Toucan对集成的测试相关驱动有进行抽象，操作者可以在同样的动作下驱动相关设备进行动。

![FixtureDebug](./src/FixtureDebug.png)

![Driver_RelayArray](./src/RelayArrayDebug.png)

特别的，针对三菱的FX PLC, 也提供了调试工具

![mit](./src/MitsubishiDebug.png)

### Toolbox联动

## 应用场景

### 常规

#### Teststand程序

- 并行测试

- 批量测试

#### AP程序

- 乒乓测试 在旋转门，或PCBA测试中，经常涉及乒乓测试方式，复用APx仪器，提高设备驾动率。在Toucan V2R0中，直接通过在配置文件中设置槽位数量，即可自动完成相关设置

#### Teststand + AP

- 并行测试。这种模式下，TestStand可以按照并行进行测试，但AP因为只能单线程跑的模式，测试执行会在AP的位置阻塞。即如果测试程序中，在TestStand程序中对AP测试的前处理部分有对其他槽位产生影响的，应当在TestStand程序中加锁设置保护区

### 自动扫码

在有些工站中，使用了自动扫码枪来触发测试，或者其他方式的不使用UI界面入口的条码输入进行测试。这种情况下，测试流程与正常流程会略有差别，其测试启动与条码确认的位置对调。

一般情况情况下，测试在接收产品条码后，判断该产品是否需要测试（UUT Identified事件），在SFCs阻塞或条码规则有问题的情况下，测试流程是不开启的(即PreUUT和PostUUT不会涉及执行)，但自动扫码时，程序要自行阻塞测试，监听是否有条码在测试，（一般情况下也需要监测到箱子的关闭或DUT到位，即PreUUT的动作在识别条码之前）

- 案例：LVHM，PCBA中带自动扫描枪的工站

- 特殊情况：LVHM，输入的条码实际会栈板编号，需要通过SFCs的API获取转换为正常的条码编号

TODO

### 设备复用

与一般项目专用的测试工站相对的，是多项目复用生产线的产品，这类产品往往是相对比较标准化的产品，或者是装备工程师对不同产品的测试需求进行综合考虑。目前Tymphany具备类似形态的有喇叭的生产线，系统的LVHM线。

- 工站编号限定。在复用产线时，有时需要限定测试程序必须在该机台下测试

### 金样挑选

在产品批量生产中，生产的产品性能会分布在一个区间，在测试设备中我们会设置测试规格区间，作为判定产品合格的条件，一般情况下，这个limit是按照CPK 1-1.33给出，即在假定目标参数符合正太分布的情况，3到4个标准差范围内的产品为合格品。

这个范围对于生产是正常的，但是对金样，考虑到参数随时间的变化，以及送样对产品一致性的要求更高，我们往往需要用更严格的规格来对获得更好参数的产品。

Toucan中支持对测试程序增加金样规格，在产品判断合格后，应用该规格检测合格的产品是否符合金样条件，符合的情况下，弹出对话框，提示操作者将样品保留

### 产品分级

在生产的产品中，可能面临如下几个可能的状况

1. 生产的瑕疵品（功能正常，性能不足或外观缺陷等原因），仍然有目标客户

2. 产品有配对或分组需求，比如耳机，音响内的高音喇叭之类成组装配的产品，一组的产品内其核心零件性能接近，可能获得更高的终端产品性能

Toucan中支持对测试规格增加次要规格（可以一次添加，即次要规格的次要规格），在产品判断不合格后，判断对应次要规格是否合格，最终返回符合的次要规格的对应等级，或者判断为不合格品。

_注意：测试规格是依次判断，即如果主规格包含次要规格，分级会没有意义。而应当是规格依次变宽（容许瑕疵）或者依次移动（数据分组）_

- 案例：类比以前APGUI的Waive功能

操作步骤：

1. 在工程设置的配置界面，点击Restrict Limit旁边的Edit按钮，（产品分级要求严格约束规格）

2. 在弹出个规格编辑界面中，选中需要添加维护的测试项目，右键点击
   
   ![SecondarySpec](./src/SecondarySpec.png)

3. 在右上角维护规格的等级标签，测试结果将会映射到对应等级会进行显示，以替换Passed。

### 引擎开发

#### 接口定义

##### IEngine

##### IExecution

##### IScript

#### 事件回调

=====__以下内容未Toucan V1 版本相关__ ====================

## 开发指南

### 新项目

Toucan软件目标为完全兼容TestStand默认开发方式(即Sequential/Parallel/Batch)，所以新项目可与基于此进行开发

其中若Sequence当前目录下存在System.xml文件，Toucan将使用Toucan模式加载运行Sequence，否则将仅以Original模式加载运行

两种模式，调试均在TestStand下进行，运行时Model调用均使用默认Model

- Toucan模式包含了如下功能
1. 集成条码输入。将屏蔽默认对话框交互（条码输入，结果显示）
2. 集成SFCs流程控制 
3. 集成报告操作
   1. 标准化报告名称
   2. 按天生成合并csv报表
   3. 提交报告至服务器中备份
4. 规格管控（可选）
5. 测试结果汇总显示
6. TestStand交互日志
- Orignial 模式功能
1. 测试结果汇总显示
2. TestStand交互日志

#### 条码获取

同TestStand默认开发，可在PreUUT  Cleanup中通过表达式

```
FileGlobals.SN = Parameters.UUT.SerialNumber    //Recommanded
```

也可在MainSequence中

```
#NoValidation(FileGlobals.SN = RunState.Caller.Locals.UUT.SerialNumber)  //Not Recommanded
```

#### SFCs相关

仅在Toucan模式下有效. 其中涉及从Toucan中获取SFCs字段的的表达式，需要在Precondition中增加表达式

```
PropertyExists("RunState.Caller.Locals.UUT.AdditionalData.IsSFC")
```

用以避免在TestStand下调试时报错

##### 获取SFCs状态

软件执行过程中，Toucan将注入如下变量，可以在MainSequence、PreUUT 、PostUUT、PreUUTLoop、PostUUTLoop中获取。为True表示开启SFCs，为False表示关闭SFCs

```
RunState.Caller.Locals.UUT.AdditionalData.IsSFC             //boolean
```

##### 附加数据提交

软件执行过程中，Toucan将注入如下变量，可以在MainSequence、PreUUT 、PostUUT、PreUUTLoop、PostUUTLoop中获取与设置

```
RunState.Caller.Locals.UUT.AdditionalData.SFCs_ExtColumn    //string
RunState.Caller.Locals.UUT.AdditionalData.SFCs_ExtValue     //string
RunState.Caller.Locals.UUT.AdditionalData.SFCs_BarcodePart  //string
```

实际使用中

可在PreUUTLoop中增加表达式 (列名只需设置一次)

```
#NoValidation(RunState.Caller.Locals.UUT.AdditionalData.SFCs_ExtColumn=",Col1,Col2")
```

在MainSequence的CleanUp中增加表达式

```
#NoValidation(RunState.Caller.Locals.UUT.AdditionalData.SFCs_ExtValue=",Val1,Val2"),
#NoValidation(RunState.Caller.Locals.UUT.AdditionalData.SFCs_BarcodePart="value1@item1;value2@item2"),
```

__注意__ : 若添加额外上传数据，其列名与结果均需以"，"(英文逗号)开头

##### 获取SFCs数据

需程序自行调用SFCs API处理

##### SFCs数据查看

在最终生成的Report中，将添加提交给SFCs的数据，如下图所示

![SFCs Report](src\sfcsreport.png)

更详细的SFCs日志，可在Log中查看，如下所示

```log
2021-11-06 07:59:19,890 [22] DEBUG TF_SFC -GetPartNo params:TYDC_CHIBI|AAK04680679
2021-11-06 07:59:20,929 [22] DEBUG TF_SFC -GetPartNo rtn: T910300006230,B401
2021-11-06 07:59:20,933 [22] DEBUG TF_SFC -CheckStation params:TYDC_CHIBI, Final_RF_Test, AAK04680679, B401, 1.0
2021-11-06 07:59:21,089 [22] DEBUG TF_SFC -CheckStation rtn: Y

2021-11-06 07:59:54,162 [22] DEBUG TF_SFC -InsertIntoTable params:TYDC_CHIBI|MES_Transfer_CHIBI_Final_RF_Test|BARCODE_PART,BARCODE,Model_NO,Line_Code,Station_Code,PROCESS_NO,Result,Defect_Code|,AAK04680679,T910300006230,B401,Final_RF_Test,Final_RF_Test,PASS,
2021-11-06 07:59:54,256 [22] DEBUG TF_SFC -InsertIntoTable rtn: Y
```

##### 外部SFCs集成

外部SFCs需要二次开发才能被Toucan所调用. 如示例所示，其需继承TYMSFC.ISFC的接口

```c#
public class VenderSFC : TYMSFC.ISFC
    {
        public string CheckStationPass(string Product, string Station, string Barcode, string LineNo, string Version)
        {
            throw new NotImplementedException();
        }

        public string GetDate(string Product)
        {
            throw new NotImplementedException();
        }

        public string GetPartNo(string product, string sn)
        {
            throw new NotImplementedException();
        }

        public string GetSpecialValue(string Product, string Type, string Parameters)
        {
            throw new NotImplementedException();
        }

        public string InsertIntoTable(string Product, string TableName, string ColumnList, string ValueList)
        {
            throw new NotImplementedException();
        }

        public string UpdateValues(string Product, string TableName, string UpdateValues, string WhereValues)
        {
            throw new NotImplementedException();
        }
    }
```

#### Defect Code 错误代码

Toucan将按照如下规则解析识别Defect Code

1. Main Sequence中的Test Item的Comments中若以@符号开头，则表示其为错误代码定义
   1. 对于Pass/Fail Test, Numeric Test, String Test, @符号后至","或回车换行前的字符串，将被识别为错误代码
   2. 对于Multiple Numeric Test, 自定义错误代码将不生效
      1. 避免增删子项导致问题
      2. 避免对错误代码有约束条件
2. 对于Sequence Call的情况，其错误代码将不生效

#### 特殊情况

##### 条码限制

Sequence可在system.xml中限制测试条码格式。限制方式为正则表达式

```xml
<RE_SerialNumber>^(\w{14}|\w{2})$</RE_SerialNumber><!--Regular Expression for SN. In Batch mode. the attr re_subsn is to match the data in MainSN. the attr subsn means how the subsn generated-->
```

上述例子为14或2位字母条码。 __注意__：默认表达式__不__支持__中划线__与__下划线__条码

对于更严格的条码限制要求，如下例所示

```xml
<RE_SerialNumber>^(XSD\-.{12}|\w{2})$</RE_SerialNumber><!--例如XSD-1234567890ab-->
```

程序默认2位或以下的条码不通过SFCs系统

##### 自定义条码输入

若有需求自定义条码输入，比如实现更佳的人机交互方式，又如自动条码枪的使用等，可以在System.xml中进行如下配置

```xml
<CustomizeInputSn>True</CustomizeInputSn><!--If true, the Check Station will be trigger after PreUUT-->
```

在该模式下，SFCs交互流程会发生改变。获取料号与检查过站将会在PreUUT执行完之后执行，如果中间发生异常（返回不符合格式，API报错等），将会在执行问PostUUT后进入下一次测试，PreUUT与PostUUT中间的所有回调将__不会__被执行（包括MainSequence）。

因此，自定义条码输入的逻辑__只能__在PreUUT内实现。

另建议在ModelOptions如下表达式禁用默认自带的UI交互（Toucan执行时会自动注入）

```
Parameters.ModelOptions.ParallelModel_ShowUUTDlg = False,
Parameters.ModelOptions.BringUUTDlgToFrontOnChange = False,
```

##### 指定Model

如果不能确保产线电脑的TestStand Model设定能符合测试需求，需在Sequence文件属性中指定Model

点击菜单Edit -> Sequence File Properties... --> Advanced页，在Model Option中选择Required Specific Model

![SpecifyModel](src/specifymodel.png)

#### 使用约束

##### 测试项目重名

在SFCs Upload Data为True的时候，测试项目要求严格的拒绝重名。因当前机制为按照当前测试项目名称生成SFCs Column Header，即特别对于Sub Sequence和Multiple Numeric Test中的项目，其子级项目名称必须避免重名。否则会导致SFCs无法插入数据。

在一般情况下，同级测试项目的重名可能会导致测试数据的误解。因此建议测试项目中避免项目的重名。

##### Sequence版本

使用Toucan后，会修改TestStand的默认Sequence文件版本策略 -->使能自动版本升级。即在Sequence文件发生任何变更时，都将导致Sequence版本变更。在未使用TYM Spec导入Limits的情况下，该变更将会产生如下影响

1. Toucan UI界面抬头显示的Sequence版本变化 （IPQC可能需要点检该项）
2. TYM Report会拆分文件

#### 推荐范式

为了便于测试程序的快速开发，以及提高代码的可读性、可维护性以及长期的标准化。建议开发者参照推荐的范式实现测试程序开发，

总体原则：

1. 避免在测试程序中使用TestStand系统底层变量。如RunState，特别是在处理系统结论相关的一些属性。如清除SequenceFail标识等。

2. 减少过多的Step的Expression, Precondition的使用。
   
   例如一个Step包含了Loop，Expression, Precondition, 以及类似PostAction, Tracing, IgnoreError的设置，将会使维护者不能直观的了解到这个Step做的事情，建议将Precondition转换为 If结构，可行的情况下将 Expression转换为Step(Expression)

3. 在非必需情况下，不建议使用Engine Callback预计PostAction, Goto等跳转设置。

4. 不要额外设置报告名称宏

5. 日期设置，使用yyyy/MM/dd，时间使用HH:mm:ss, 不要使用AM/AM之类的设置

##### 推荐设置

1. Deploy至服务器的程序，需要使用TestStand DeployUtility进行打包操作，避免路径依赖等相关问题
2. TestStand Sequence均需要加密，密码建议使用通用密码TymPte
3. 配置文件放置于Sequence文件同级目录的Config文件夹下

##### 异常处理

一般情况下，建议设置Engine的异常处理模式建议为遇异常跳转至Cleanup（即RTEOption_Continue），在调试阶段，可以设置为弹出异常对话框。

可在System.xml中进行配置。（如果测试程序有做相关设置，则测试程序的设置为生效设置）

```xml
<ErrorHandle>1<!--0: RTEOption_ShowDialog/1: RTEOption_Continue/2: RTEOption_Ignore/3: RTEOption_Abort/4: RTEOption_Retry. For TestStand Only--></ErrorHandle>
```

Toucan会在测试执行完成时收集异常信息并显示在界面上（如正常测试的动作异常），同时也会在Execution退出时收集异常信息并显示在界面上（如PreUUTLoop中的仪器初始化异常）。若设置为Abort时，则会导致Execution被立即中止，无法完成收集异常的动作。

步骤触发异常，可使用如下表达式手工维护异常状态

```
Step.Result.Error.Occurred = (Random(0, 1) > 0.7),
Step.Result.Error.Occurred ? (Step.Result.Error.Msg = NameOf(Step), Step.Result.Error.Code = -101 ): Nothing
```

###### Action Loop

一般在仪器/工具板/驱动工作不稳定的时候，会有多次尝试执行Action的需求，此时，若直接对返回的非期望值做报错处理，会导致直接被跳转至Cleanup中，而不执行Loop动作

此时可以在Loop页，在LoopType选择PassFail Count后再选择Custome。然后进行如下修改

1. 在Loop While Expression中，将Runstate.LoopIndex <= 最大允许次数， 将RunState.LoopNumPassed < 10 替换为非预期判定
2. 在Loop Status Expression中，将RunState.LoopNumPassed >=10 替换为预期判定

![Action Loop](src/actionloop.png)

##### 测试项目

测试项目结构建议与TO（Test Plan/ Test Overview/TCD）保持一致，即MainSequence包含所有一级测试项目，子级项目以SubSequence或Multiple Numeric Test的形式存在

__注：__Multiple Numeric Test不建议设置成为包含测试项目的SubSequence。因为不便于划分其项目层级

###### Meta Data 元数据

如MAC, Key, ID等DUT元数据，一般TO要求为比较写入与读取是否一致，即测试工站需要保证的测试内容为读写一致。故该类项目应当认定为PassFail 类测试。

但基于相关方需求，将该类信息可在客户感知报表中体现，保留其动态规格的设置方式，即Limit中设置为写入的数据，Source中为读取的数据。__注：__使用该种设置方式，测试结果为实际MetaData，因无法判断写入读取是否一致，在数据分析工具Dove中，将被识别为LOG，而不进行可视化处理

###### 边界处理

在模拟量数据比较是，避免使用大于0，小于0的操作。容易导致问题

1. 电压差。因为线损，串扰，浮地等原因，0点附近电压可能会在正负区间抖动，导致误判
2. 数据精度问题。模拟量的精度显示，可能导致因为圆整的问题将极小量还原为0导致误判

## Calibration / Reference / Verification / Correlation

测试程序，结合测试硬件，作为测试装备。其用以检测检验被测物的量化指标或对被测物进行分类处理。常规情况下，我们使用MSA来评估测试系统是否有效，这个有效性，很大程度体现在设备变差与人工变差在系统总变差的贡献，因此有需要对测试装备量化指标的有效性进行维护和检验，以满足生产对测试系统性能的要求。

测试数据处理流程，原始数据 --> 校准 --> 校验 --> 输出数据

例如测试BRF output power. 原始度数是-30dbm, 校准偏差为-27.6dbm, 校验偏差 -0.1dbm, 输出结果则为-0.3dbm

例如MIC板测试，测试1KHz rmslevel，原始读书 -40 dbm, 校准人工嘴偏差为-30dbm，校验偏差为0.3 dbm, 则输出结果应为-10.3dbm

### Calibration 校准

校准是指提升系统物理绝对准确度的过程。其表征的是单个系统实体的特性参数。该项参数与被测物无关。原则上应当由更高等级的可信设备对测试系统进行测量获取相关的校准结果

- 硬件校准、平台校准、设备校准
  
  此类校准适用于一系列的测试项目（Project），可以理解为是设备的特性参数。其不依赖于具体的测试项目配置，而且相关的测试硬件在校准周期内不应当发生改变，或者说在硬件发生改变时，都应当对设备进行校准。例如
  
  - 喇叭线复用Chamber的MIC校准
  
  - 柔性产线机械手位置标定参数

- 项目校准
  
  此类校准仅适用于特定的测试项目（project）。因为各方面的问题，测试设备的资源在项目附属硬件中会产生变异，需要额外进行校准。例如
  
  - 喇叭测试中阻抗测试回路涉及DUT侧的治具转接
  
  - RF测试中不同项目适用的夹具中扩充了RF Switch或Attenuator，阻抗特性不一致

### Reference 校样/参考

校样，与校准不一样，其关注点不在于绝对精度，而更多关注以于样品的差异。常见于Vector类数据的处理，因为相比于Scale数据，其采集更复杂，差异评估维度多样，校准相对困难。通过已经被验证样品测试指标保证量化参数的准确性，例如

- Acoustic RMS Level

- RF pathloss
  
  - 天线位置敏感，外接仪器测试需要仿形

一般情况下，适用Reference的项目，其绝对精度主要由测试样品的设备保证，不用再对测试系统该项指标做校准

_Q: 如何保证样品的测试指标是随时间变化是稳定的。样品也要有校准周期。_

一般情况下，校样是使用样品直接测试获得到原始数据，然后与真值的差异作为Reference Data。被测物测试的数据，使用Reference Data作为补偿，还原为测试正直

### Verification 验证

用以验证测试系统测试能力的时间一致性，避免时间导致的变差导致测试结果脱离预期

一般情况下，会使用更严格的规格对样品进行测试，在系统跑偏之前，触发校准和校样，恢复测试能力

### Correlation 校正/相关性

在批量生产中，测试系统会需要复制以提升产能，但复制并不会完全一致，即使经过校准，其测试特性仍会存在变差，尤其是在矢量数据中，

### V0R2 迁移

1. 导出Spec
   
   功能迁移至Toucan程序中，可在加载Sequence文件后，点击菜单Tools --> Export Spec...

2. 自定义条码输入
   
   1. 修改System.xml中<CustomizeInputSn>True</CustomizeInputSn>
   2. 将基于V0R2的Sequence TYM_CustomizedInputSN内容，移动至PreUUT中

3. 获取SFCs相关
   
   1. TYM_DATA_GetSysParam中
      
      1. 在获取当前SFCs状态（是否过SFC），参见[SFCs相关获取SFCs状态](#####获取SFCs状态)
      2. 其他相关信息将被取消
   
   2. TYM_DATA_Setting中
      
      1. 设置SFCs 附加列。参见[SFCs附加数据提交](#####附加数据提交)将Cleanup中步骤Extend Cols中Extend_ColumnList的内容赋值给SFCs_ExtColumn
   
   3. TYM_Data_UpdateData
      
      设置SFCs附加数据和BarcodePart，参见[SFCs附加数据提交](#####附加数据提交)进行更新

__注意：__V1R0需要使用TestStand 默认的Model进行测试，若使用了V0R2中的Model，将会导致报告，SFCs等动作被执行多次。
