//**********************************************************************
//name:         GUA_Led.c        
//introduce:    ����Զ����LED����   
//author:       ����Ĵ����      
//email:        897503845@qq.com   
//QQ group:     ���BLE֮CC2640R2F(557278427)
//shop:
//https://shop217632629.taobao.com/?spm=2013.1.1000126.d21.hd2o8i
//changetime:   2017.10.17
//**********************************************************************
#include <string.h>
#include <stdio.h>

#include <ti/sysbios/knl/Task.h>
#include <ti/sysbios/knl/Clock.h>
#include <ti/sysbios/knl/Semaphore.h>
#include <ti/sysbios/knl/Queue.h>

#include <ti/drivers/PIN.h>

#include "board.h"
#include "GUA_Led.h"

/*********************�궨��************************/   
//LEDS
#define GUA_LED1     PIN_ID(0)
#define GUA_EN1     PIN_ID(3)
//#define GUA_EN2     PIN_ID(2)
//#define GUA_EN3     PIN_ID(3)
//#define GUA_EN4     IOID_4
//#define GUA_A0     PIN_ID(5)
//#define GUA_A1     PIN_ID(6)
//#define GUA_A2     PIN_ID(7)

/*********************�ڲ�����************************/  
static PIN_State  sGUA_LedPins;
static PIN_Handle sGUA_HledPins = NULL;

//IO����
PIN_Config bGUA_LedPinsCfg[] =
{
    GUA_LED1 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
    GUA_EN1 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    GUA_EN2 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    GUA_EN3 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    GUA_EN4 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    GUA_A0 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    GUA_A1 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    GUA_A2 | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
    PIN_TERMINATE
};

//**********************************************************************
//name:         GUA_Led_Set
//introduce:    ��ϵ������
//parameter:    nGUA_Led_No��GUA_LED_NO_1��GUA_LED_NO_2��GUA_LED_NO_3��
//              GUA_LED_NO_4��GUA_LED_NO_ALL
//              nGUA_Mode��GUA_LED_MODE_OFF��GUA_LED_MODE_ON��
//              GUA_LED_MODE_FLASH��GUA_LED_MODE_TOGGLE
//return:       none
//author:       ����Ĵ����
//email:        897503845@qq.com
//QQ group:     ���BLE֮CC2640R2F(557278427)
//shop:
//https://shop217632629.taobao.com/?spm=2013.1.1000126.d21.hd2o8i
//changetime:   2017.10.17
//**********************************************************************
void GUA_Led_Set(GUA_U8 nGUA_Led_No, GUA_U8 nGUA_Mode)
{
    GUA_U8 nGUA_I = 0;
    GUA_U8 nbGUA_Pin[8] = {GUA_LED1, GUA_EN1};//, GUA_EN2, GUA_EN3, GUA_EN4, GUA_A0, GUA_A1, GUA_A2};

    //��һ��ʹ��ʱע��IO
    if(NULL == sGUA_HledPins)
    {
        sGUA_HledPins = PIN_open(&sGUA_LedPins, bGUA_LedPinsCfg);
    }

    //���μ��4��led�Ƿ���ִ�ж���
    for(nGUA_I = 0; nGUA_I <= 7; nGUA_I++)
    {
        if(nGUA_Led_No & (1 << nGUA_I))
        {
            //ִ��ģʽ
            switch(nGUA_Mode)
            {
                //�ص�ģʽ
                case GUA_LED_MODE_OFF:
                {
                    PIN_setOutputValue(sGUA_HledPins, nbGUA_Pin[nGUA_I], 0);
                    break;
                }

                //����ģʽ
                case GUA_LED_MODE_ON:
                {
                    PIN_setOutputValue(sGUA_HledPins, nbGUA_Pin[nGUA_I], 1);
                    break;
                }

                //��������һ��
                case GUA_LED_MODE_FLASH:
                {
                    PIN_setOutputValue(sGUA_HledPins, nbGUA_Pin[nGUA_I], 1);
                    Task_sleep(10*1000/Clock_tickPeriod);
                    PIN_setOutputValue(sGUA_HledPins, nbGUA_Pin[nGUA_I], 0);
                    break;
                }

                //��תһ��
                case GUA_LED_MODE_TOGGLE:
                {
                    PIN_setOutputValue(sGUA_HledPins, nbGUA_Pin[nGUA_I], !PIN_getOutputValue(nbGUA_Pin[nGUA_I]));
                    break;
                }
            }
        }
    }
}
