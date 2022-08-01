//**********************************************************************
//name:         GUA_Led.h        
//introduce:    香瓜自定义的LED驱动头文件
//author:       甜甜的大香瓜      
//email:        897503845@qq.com   
//QQ group:     香瓜BLE之CC2640R2F(557278427)
//shop:
//https://shop217632629.taobao.com/?spm=2013.1.1000126.d21.hd2o8i
//changetime:   2017.09.20
//**********************************************************************
#ifndef _GUA_LED_H_
#define _GUA_LED_H_

/*********************宏定义************************/ 
//类型宏
#ifndef GUA_U8
typedef unsigned char GUA_U8;
#endif

//LEDS
#define GUA_LED_NO_1           0x01
#define GUA_EN_NO_1            0x02
//#define GUA_EN_NO_2            0x04
//#define GUA_EN_NO_3            0x08
//#define GUA_EN_NO_4            0x10
//#define GUA_A_NO_0             0x20
//#define GUA_A_NO_1             0x40
//#define GUA_A_NO_2             0x80

//Modes
#define GUA_LED_MODE_OFF        0x00
#define GUA_LED_MODE_ON         0x01
#define GUA_LED_MODE_FLASH      0x02
#define GUA_LED_MODE_TOGGLE     0x04

/*********************函数声明************************/ 
extern void GUA_Led_Set(GUA_U8 nGUA_Led_No, GUA_U8 nGUA_Mode);

#endif
