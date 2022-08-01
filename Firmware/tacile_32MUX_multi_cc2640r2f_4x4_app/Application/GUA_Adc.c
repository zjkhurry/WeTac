//**********************************************************************
//name:         GUA_Adc.c
//introduce:    香瓜自定义的ADC驱动
//author:       甜甜的大香瓜      
//email:        897503845@qq.com   
//QQ group:     香瓜BLE之CC2640R2F(557278427)
//shop:         opengua.taobao.com
//changetime:   2018.10.15
//**********************************************************************
#include <stdint.h>
#include <stdbool.h>

#include <ti/drivers/ADC.h>
#include <ti/drivers/PIN.h>
#include <ti/drivers/pin/PINCC26XX.h>
#include <ti/drivers/Power.h>
#include <ti/drivers/power/PowerCC26XX.h>
#include <ti/devices/DeviceFamily.h>

#include <ti/drivers/ADC.h>
#include <ti/drivers/adc/ADCCC26XX.h>
#include "board.h"

#include "GUA_Adc.h"

/*********************宏定义************************/
//定义ADC管脚
#define GUA_CC2640R2_ADC7_ANALOG          IOID_5

/*
#define GUA_CC2640R2_ADC6_ANALOG          IOID_24
#define GUA_CC2640R2_ADC5_ANALOG          IOID_25
#define GUA_CC2640R2_ADC4_ANALOG          IOID_26
#define GUA_CC2640R2_ADC3_ANALOG          IOID_27
#define GUA_CC2640R2_ADC2_ANALOG          IOID_28
#define GUA_CC2640R2_ADC1_ANALOG          IOID_29
#define GUA_CC2640R2_ADC0_ANALOG          IOID_30
*/

/*********************内部变量************************/
//ADC目标
ADCCC26XX_Object stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADCCOUNT];

//ADC属性
const ADCCC26XX_HWAttrs cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADCCOUNT] = {
    {
         .adcDIO              = GUA_CC2640R2_ADC7_ANALOG,
         .adcCompBInput       = ADC_COMPB_IN_AUXIO7,
         .refSource           = ADCCC26XX_FIXED_REFERENCE,
         .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
         .inputScalingEnabled = true,
         .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
         .returnAdjustedVal   = 0
    },
    /*
    {
        .adcDIO              = GUA_CC2640R2_ADC6_ANALOG,
        .adcCompBInput       = ADC_COMPB_IN_AUXIO6,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = GUA_CC2640R2_ADC5_ANALOG,
        .adcCompBInput       = ADC_COMPB_IN_AUXIO5,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = GUA_CC2640R2_ADC4_ANALOG,
        .adcCompBInput       = ADC_COMPB_IN_AUXIO4,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = GUA_CC2640R2_ADC3_ANALOG,
        .adcCompBInput       = ADC_COMPB_IN_AUXIO3,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = GUA_CC2640R2_ADC2_ANALOG,
        .adcCompBInput       = ADC_COMPB_IN_AUXIO2,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = GUA_CC2640R2_ADC1_ANALOG,
        .adcCompBInput       = ADC_COMPB_IN_AUXIO1,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = GUA_CC2640R2_ADC0_ANALOG,
        .adcCompBInput       = ADC_COMPB_IN_AUXIO0,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
*/
    {
        .adcDIO              = PIN_UNASSIGNED,
        .adcCompBInput       = ADC_COMPB_IN_DCOUPL,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = PIN_UNASSIGNED,
        .adcCompBInput       = ADC_COMPB_IN_VSS,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    },
    {
        .adcDIO              = PIN_UNASSIGNED,
        .adcCompBInput       = ADC_COMPB_IN_VDDS,
        .refSource           = ADCCC26XX_FIXED_REFERENCE,
        .samplingDuration    = ADCCC26XX_SAMPLING_DURATION_2P7_US,
        .inputScalingEnabled = true,
        .triggerSource       = ADCCC26XX_TRIGGER_MANUAL,
        .returnAdjustedVal   = 0
    }
};

//ADC配置
const ADC_Config cstGUA_ADC_Config[GUA_CC2640R2_ADCCOUNT] = {
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC7], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC7]},
    /*
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC6], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC6]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC5], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC5]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC4], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC4]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC3], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC3]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC2], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC2]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC1], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC1]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADC0], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADC0]},
    */
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADCDCOUPL], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADCDCOUPL]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADCVSS], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADCVSS]},
    {&ADCCC26XX_fxnTable, &stGUA_AdcCC26xxObjects[GUA_CC2640R2_ADCVDDS], &cstGUA_AdcCC26xxHWAttrs[GUA_CC2640R2_ADCVDDS]},
};

const GUA_U8 cgnGUA_ADC_Count = GUA_CC2640R2_ADCCOUNT;

//ADC句柄
static ADC_Handle sstGUA_ADC_Handle[GUA_CC2640R2_ADCCOUNT];

//ADC参数
static ADC_Params sstGUA_ADC_Params[GUA_CC2640R2_ADCCOUNT];

//**********************************************************************
//name:         GUA_ADC_Init
//introduce:    香瓜ADC初始化函数
//parameter:    none
//return:       none
//author:       甜甜的大香瓜
//email:        897503845@qq.com
//QQ group:     香瓜BLE之CC2640R2F(557278427)
//shop:         opengua.taobao.com
//changetime:   2018.10.15
//**********************************************************************
void GUA_ADC_Init(void)
{
    GUA_U8 nGUA_ADC_Count;

    for(nGUA_ADC_Count = 0; nGUA_ADC_Count < GUA_CC2640R2_ADCCOUNT; nGUA_ADC_Count++)
    {
        cstGUA_ADC_Config[nGUA_ADC_Count].fxnTablePtr->initFxn((ADC_Handle)&cstGUA_ADC_Config[nGUA_ADC_Count]);
        ADC_Params_init(&sstGUA_ADC_Params[nGUA_ADC_Count]);
    }
}

//**********************************************************************
//name:         GUA_ADC_Open
//introduce:    打开ADC驱动和配置驱动
//parameter:    nGUA_ADC_Channel：GUA_CC2640R2_ADC7、GUA_CC2640R2_ADC6……GUA_CC2640R2_ADC0
//return:       none
//author:       甜甜的大香瓜
//email:        897503845@qq.com
//QQ group:     香瓜BLE之CC2640R2F(557278427)
//shop:         opengua.taobao.com
//changetime:   2018.10.15
//**********************************************************************
void GUA_ADC_Open(GUA_U8 nGUA_ADC_Channel)
{
    sstGUA_ADC_Handle[nGUA_ADC_Channel] = cstGUA_ADC_Config[nGUA_ADC_Channel].fxnTablePtr->openFxn((ADC_Handle)&cstGUA_ADC_Config[nGUA_ADC_Channel],&sstGUA_ADC_Params[nGUA_ADC_Channel]);
}

//**********************************************************************
//name:         GUA_ADC_Read
//introduce:    香瓜ADC读取函数
//parameter:    nGUA_ADC_Channel：GUA_CC2640R2_ADC7、GUA_CC2640R2_ADC6……GUA_CC2640R2_ADC0
//return:       16bit的ADC读取结果
//author:       甜甜的大香瓜
//email:        897503845@qq.com
//QQ group:     香瓜BLE之CC2640R2F(557278427)
//shop:         opengua.taobao.com
//changetime:   2018.10.15
//**********************************************************************
GUA_U16 GUA_ADC_Read(GUA_U8 nGUA_ADC_Channel)
{
    GUA_U16 nGUA_ADC_Result;
    cstGUA_ADC_Config[nGUA_ADC_Channel].fxnTablePtr->convertFxn(sstGUA_ADC_Handle[nGUA_ADC_Channel], &nGUA_ADC_Result);
    return nGUA_ADC_Result;
}
