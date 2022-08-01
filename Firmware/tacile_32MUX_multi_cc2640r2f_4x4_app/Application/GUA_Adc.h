//**********************************************************************
//name:         GUA_Adc.h
//introduce:    香瓜自定义的ADC驱动头文件
//author:       甜甜的大香瓜      
//email:        897503845@qq.com   
//QQ group:     香瓜BLE之CC2640R2F(557278427)
//shop:         opengua.taobao.com
//changetime:   2018.10.15
//**********************************************************************
#ifndef _GUA_ADC_H_
#define _GUA_ADC_H_

/*********************宏定义************************/
//类型宏
#ifndef GUA_C
typedef char GUA_C;
#endif

#ifndef GUA_U8
typedef unsigned char GUA_U8;
#endif

#ifndef GUA_8
typedef signed char GUA_8;
#endif

#ifndef GUA_U16
typedef unsigned short GUA_U16;
#endif

#ifndef GUA_16
typedef signed short GUA_16;
#endif

#ifndef GUA_U32
typedef unsigned long GUA_U32;
#endif

#ifndef GUA_32
typedef signed long GUA_32;
#endif

#ifndef GUA_U64
typedef unsigned long long GUA_U64;
#endif

#ifndef GUA_64
typedef signed long long GUA_64;
#endif

/*********************外部变量************************/
//ADC号
typedef enum GUA_CC2640R2_ADCName {
    GUA_CC2640R2_ADC7 = 0,
    /*
    GUA_CC2640R2_ADC6,
    GUA_CC2640R2_ADC5,
    GUA_CC2640R2_ADC4,
    GUA_CC2640R2_ADC3,
    GUA_CC2640R2_ADC2,
    GUA_CC2640R2_ADC1,
    GUA_CC2640R2_ADC0,
    */
    GUA_CC2640R2_ADCDCOUPL,
    GUA_CC2640R2_ADCVSS,
    GUA_CC2640R2_ADCVDDS,

    GUA_CC2640R2_ADCCOUNT
}GUA_CC2640R2_ADCName;


/*********************函数声明************************/
extern void GUA_ADC_Init(void);
extern void GUA_ADC_Open(GUA_U8 nGUA_ADC_Channel);
extern GUA_U16 GUA_ADC_Read(GUA_U8 nGUA_ADC_Channel);

#endif
