/*
 * Copyright (c) 2015-2016, Texas Instruments Incorporated
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * *  Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * *  Neither the name of Texas Instruments Incorporated nor the names of
 *    its contributors may be used to endorse or promote products derived
 *    from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/** ============================================================================
 *  @file       CC2640R2DK_4XS.h
 *
 *  @brief      CC2640R2DK_4XS Board Specific header file.
 *
 *  The CC2640R2DK_4XS header file should be included in an application as
 *  follows:
 *  @code
 *  #include "CC2640R2DK_4XS.h"
 *  @endcode
 *
 *  This board file is made for the 4x4 mm QFN package, to convert this board
 *  file to use for other smaller device packages please refer to the table
 *  below which lists the max IOID values supported by each package. All other
 *  unused pins should be set to IOID_UNUSED.
 *
 *  Furthermore the board file is also used
 *  to define a symbol that configures the RF front end and bias.
 *  See the comments below for more information.
 *  For an in depth tutorial on how to create a custom board file, please refer
 *  to the section "Running the SDK on Custom Boards" with in the Software
 *  Developer's Guide.
 *
 *  Refer to the datasheet for all the package options and IO descriptions:
 *  http://www.ti.com/lit/ds/symlink/cc2640r2f.pdf
 *
 *  +-----------------------+------------------+-----------------------+
 *  |     Package Option    |  Total GPIO Pins |   MAX IOID            |
 *  +=======================+==================+=======================+
 *  |     7x7 mm QFN        |     31           |   IOID_30             |
 *  +-----------------------+------------------+-----------------------+
 *  |     5x5 mm QFN        |     15           |   IOID_14             |
 *  +-----------------------+------------------+-----------------------+
 *  |     4x4 mm QFN        |     10           |   IOID_9              |
 *  +-----------------------+------------------+-----------------------+
 *  |     2.7 x 2.7 mm WCSP |     14           |   IOID_13             |
 *  +-----------------------+------------------+-----------------------+
 *  ============================================================================
 */
//#ifndef __CC2640R2DK_4XS_BOARD_H__
#define __CC2640R2DK_4XS_BOARD_H__

#ifdef __cplusplus
extern "C" {
#endif

/* Includes */
#include <ti/drivers/PIN.h>
#include <ti/devices/cc26x0r2/driverlib/ioc.h>

/* Externs */
extern const PIN_Config BoardGpioInitTable[];

/* Defines */
#ifndef CC2640R2DK_4XS
  #define CC2640R2DK_4XS
#endif /* CC2640R2DK_4XS */

/*
 *  ============================================================================
 *  RF Front End and Bias configuration symbols for TI reference designs and
 *  kits. This symbol sets the RF Front End configuration in ble_user_config.h
 *  and selects the appropriate PA table in ble_user_config.c.
 *  Other configurations can be used by editing these files.
 *
 *  Define only one symbol:
 *  CC2650EM_7ID    - Differential RF and internal biasing
                      (default for CC2640R2 LaunchPad)
 *  CC2650EM_5XD    锟� Differential RF and external biasing
 *  CC2650EM_4XS    锟� Single-ended RF on RF-P and external biasing
 *  CC2640R2DK_CXS  - WCSP: Single-ended RF on RF-N and external biasing
 *                    (Note that the WCSP is only tested and characterized for
 *                     single ended configuration, and it has a WCSP-specific
 *                     PA table)
 *
 *  Note: CC2650EM_xxx reference designs apply to all CC26xx devices.
 *  ==========================================================================
 */
#define CC2650EM_4XS

/* Mapping of pins to board signals using general board aliases
 *      <board signal alias>                <pin mapping>
 */

/* Button Board */
#define CC2640R2DK_4XS_KEY_SELECT                    PIN_UNASSIGNED//IOID_7         /* P1.14 */
#define CC2640R2DK_4XS_KEY_UP                        PIN_UNASSIGNED//IOID_4         /* P1.10 */
#define CC2640R2DK_4XS_KEY_DOWN                      PIN_UNASSIGNED//IOID_3         /* P1.12 */
#define CC2640R2DK_4XS_KEY_LEFT                      PIN_UNASSIGNED
#define CC2640R2DK_4XS_KEY_RIGHT                     PIN_UNASSIGNED


/* Analog Capable DIO's */
#define CC2640R2DK_4XS_DIO23_ANALOG                  PIN_UNASSIGNED//IOID_5
#define CC2640R2DK_4XS_DIO24_ANALOG                  PIN_UNASSIGNED//IOID_6
#define CC2640R2DK_4XS_DIO25_ANALOG                  PIN_UNASSIGNED//IOID_7
#define CC2640R2DK_4XS_DIO26_ANALOG                  PIN_UNASSIGNED//IOID_8
#define CC2640R2DK_4XS_DIO27_ANALOG                  PIN_UNASSIGNED//IOID_9

/* GPIO */
#define CC2640R2DK_4XS_GPIO_LED_ON                   1
#define CC2640R2DK_4XS_GPIO_LED_OFF                  0

/* LEDs */
#define CC2640R2DK_4XS_PIN_LED_ON                    1
#define CC2640R2DK_4XS_PIN_LED_OFF                   0
#define CC2640R2DK_4XS_PIN_LED1                      IOID_0 //PIN_UNASSIGNED
#define CC2640R2DK_4XS_PIN_LED2                      IOID_3 //PIN_UNASSIGNED
#define CC2640R2DK_4XS_PIN_LED3                      PIN_UNASSIGNED//IOID_5             /* P1.2  */
#define CC2640R2DK_4XS_PIN_LED4                      PIN_UNASSIGNED//IOID_6             /* P1.4  */

/* LCD  Board */
#define CC2640R2DK_4XS_LCD_MODE                      PIN_UNASSIGNED
#define CC2640R2DK_4XS_LCD_RST                       PIN_UNASSIGNED
#define CC2640R2DK_4XS_LCD_CSN                       PIN_UNASSIGNED

/* SPI Board */
#define CC2640R2DK_4XS_FLASH_CS_ON                    0
#define CC2640R2DK_4XS_FLASH_CS_OFF                   1
#define CC2640R2DK_4XS_SPI0_MISO                     PIN_UNASSIGNED//IOID_2             /* P1.20 */
#define CC2640R2DK_4XS_SPI0_MOSI                     IOID_6             /* P1.18 */
#define CC2640R2DK_4XS_SPI0_CLK                      IOID_7             /* P1.16 */
#define CC2640R2DK_4XS_SPI0_CSN                      PIN_UNASSIGNED//IOID_4
#define CC2640R2DK_4XS_SPI0_DRDY                     PIN_UNASSIGNED//IOID_1
#define CC2640R2DK_4XS_SPI0_START                    PIN_UNASSIGNED//IOID_8
/* I2C */
#define CONFIG_I2C_0                            CC2640R2_LAUNCHXL_I2C0
#define CC2640R2_LAUNCHXL_I2C0_SCL0             PIN_UNASSIGNED//IOID_7
#define CC2640R2_LAUNCHXL_I2C0_SDA0             PIN_UNASSIGNED//IOID_6

/* Power Board */
#define CC2640R2DK_4XS_3V3_EN                        PIN_UNASSIGNED

/* PWM Outputs */
#define CC2640R2DK_4XS_PWMPIN0                       CC2640R2DK_4XS_PIN_LED3
#define CC2640R2DK_4XS_PWMPIN1                       CC2640R2DK_4XS_PIN_LED4
#define CC2640R2DK_4XS_PWMPIN2                       PIN_UNASSIGNED
#define CC2640R2DK_4XS_PWMPIN3                       PIN_UNASSIGNED
#define CC2640R2DK_4XS_PWMPIN4                       PIN_UNASSIGNED
#define CC2640R2DK_4XS_PWMPIN5                       PIN_UNASSIGNED
#define CC2640R2DK_4XS_PWMPIN6                       PIN_UNASSIGNED
#define CC2640R2DK_4XS_PWMPIN7                       PIN_UNASSIGNED

/* UART Board */
#define CC2640R2DK_4XS_UART_RX                       PIN_UNASSIGNED//IOID_1             /* P1.7 */
#define CC2640R2DK_4XS_UART_TX                       PIN_UNASSIGNED//IOID_2             /* P1.9 */
#define CC2640R2DK_4XS_UART_CTS                      PIN_UNASSIGNED
#define CC2640R2DK_4XS_UART_RTS                      PIN_UNASSIGNED

/*!
 *  @brief  Initialize the general board specific settings
 *
 *  This function initializes the general board specific settings.
 */
void CC2640R2DK_4XS_initGeneral(void);

/*!
 *  @def    CC2640R2DK_4XS_ADCBufName
 *  @brief  Enum of ADCBufs
 */
typedef enum CC2640R2DK_4XS_ADCBufName {
    CC2640R2DK_4XS_ADCBUF0 = 0,

    CC2640R2DK_4XS_ADCBUFCOUNT
} CC2640R2DK_4XS_ADCBufName;

/*!
 *  @def    CC2640R2DK_4XS_ADCBuf0SourceName
 *  @brief  Enum of ADCBuf channels
 */
typedef enum CC2640R2DK_4XS_ADCBuf0ChannelName {
    CC2640R2DK_4XS_ADCBUF0CHANNEL0 = 0,
    CC2640R2DK_4XS_ADCBUF0CHANNEL1,
    CC2640R2DK_4XS_ADCBUF0CHANNEL2,
    CC2640R2DK_4XS_ADCBUF0CHANNEL3,
    CC2640R2DK_4XS_ADCBUF0CHANNEL4,
    CC2640R2DK_4XS_ADCBUF0CHANNELVDDS,
    CC2640R2DK_4XS_ADCBUF0CHANNELDCOUPL,
    CC2640R2DK_4XS_ADCBUF0CHANNELVSS,

    CC2640R2DK_4XS_ADCBUF0CHANNELCOUNT
} CC2640R2DK_4XS_ADCBuf0ChannelName;

/*!
 *  @def    CC2640R2DK_4XS_ADCName
 *  @brief  Enum of ADCs
 */
typedef enum CC2640R2DK_4XS_ADCName {
    CC2640R2DK_4XS_ADC0 = 0,
    CC2640R2DK_4XS_ADC1,
    CC2640R2DK_4XS_ADC2,
    CC2640R2DK_4XS_ADC3,
    CC2640R2DK_4XS_ADC4,
    CC2640R2DK_4XS_ADCDCOUPL,
    CC2640R2DK_4XS_ADCVSS,
    CC2640R2DK_4XS_ADCVDDS,

    CC2640R2DK_4XS_ADCCOUNT
} CC2640R2DK_4XS_ADCName;

/*!
 *  @def    CC2640R2DK_4XS_CryptoName
 *  @brief  Enum of Crypto names
 */
typedef enum CC2640R2DK_4XS_CryptoName {
    CC2640R2DK_4XS_CRYPTO0 = 0,

    CC2640R2DK_4XS_CRYPTOCOUNT
} CC2640R2DK_4XS_CryptoName;

/*!
 *  @def    CC2640R2DK_4XS_GPIOName
 *  @brief  Enum of GPIO names
 */
typedef enum CC2640R2DK_4XS_GPIOName {
    CC2640R2DK_4XS_GPIO_S1 = 0,
//    CC2640R2DK_4XS_GPIO_S2,
//    CC2640R2DK_4XS_GPIO_LED1,
//    CC2640R2DK_4XS_GPIO_LED2,
//    CC2640R2DK_4XS_GPIO_LED3,
//    CC2640R2DK_4XS_GPIO_LED4,
    CC2640R2DK_4XS_SPI0_CS,
    CC2640R2DK_4XS_SPI0_ST,

    CC2640R2DK_4XS_GPIOCOUNT
} CC2640R2DK_4XS_GPIOName;

/*!
 *  @def    CC2640R2DK_4XS_GPTimerName
 *  @brief  Enum of GPTimer parts
 */
typedef enum CC2640R2DK_4XS_GPTimerName {
    CC2640R2DK_4XS_GPTIMER0A = 0,
    CC2640R2DK_4XS_GPTIMER0B,
    CC2640R2DK_4XS_GPTIMER1A,
    CC2640R2DK_4XS_GPTIMER1B,
    CC2640R2DK_4XS_GPTIMER2A,
    CC2640R2DK_4XS_GPTIMER2B,
    CC2640R2DK_4XS_GPTIMER3A,
    CC2640R2DK_4XS_GPTIMER3B,

    CC2640R2DK_4XS_GPTIMERPARTSCOUNT
} CC2640R2DK_4XS_GPTimerName;

/*!
 *  @def    CC2640R2DK_4XS_GPTimers
 *  @brief  Enum of GPTimers
 */
typedef enum CC2640R2DK_4XS_GPTimers {
    CC2640R2DK_4XS_GPTIMER0 = 0,
    CC2640R2DK_4XS_GPTIMER1,
    CC2640R2DK_4XS_GPTIMER2,
    CC2640R2DK_4XS_GPTIMER3,

    CC2640R2DK_4XS_GPTIMERCOUNT
} CC2640R2DK_4XS_GPTimers;

/*!
 *  @def    CC2640R2DK_4XS_PWM
 *  @brief  Enum of PWM outputs
 */
typedef enum CC2640R2DK_4XS_PWMName {
    CC2640R2DK_4XS_PWM0 = 0,
    CC2640R2DK_4XS_PWM1,
    CC2640R2DK_4XS_PWM2,
    CC2640R2DK_4XS_PWM3,
    CC2640R2DK_4XS_PWM4,
    CC2640R2DK_4XS_PWM5,
    CC2640R2DK_4XS_PWM6,
    CC2640R2DK_4XS_PWM7,

    CC2640R2DK_4XS_PWMCOUNT
} CC2640R2DK_4XS_PWMName;

/*!
 *  @def    CC2640R2DK_4XS_SPIName
 *  @brief  Enum of SPI names
 */
typedef enum CC2640R2DK_4XS_SPIName {
    CC2640R2DK_4XS_SPI0 = 0,

    CC2640R2DK_4XS_SPICOUNT
} CC2640R2DK_4XS_SPIName;

/*!
 *  @def    CC2640R2_LAUNCHXL_I2CName
 *  @brief  Enum of I2C names
 */
typedef enum CC2640R2_LAUNCHXL_I2CName {
    CC2640R2_LAUNCHXL_I2C0 = 0,

    CC2640R2_LAUNCHXL_I2CCOUNT
} CC2640R2_LAUNCHXL_I2CName;

/*!
 *  @def    CC2640R2DK_4XS_UARTName
 *  @brief  Enum of UARTs
 */
typedef enum CC2640R2DK_4XS_UARTName {
    CC2640R2DK_4XS_UART0 = 0,

    CC2640R2DK_4XS_UARTCOUNT
} CC2640R2DK_4XS_UARTName;

/*!
 *  @def    CC2640R2DK_4XS_UDMAName
 *  @brief  Enum of DMA buffers
 */
typedef enum CC2640R2DK_4XS_UDMAName {
    CC2640R2DK_4XS_UDMA0 = 0,

    CC2640R2DK_4XS_UDMACOUNT
} CC2640R2DK_4XS_UDMAName;

/*!
 *  @def    CC2640R2DK_4XS_WatchdogName
 *  @brief  Enum of Watchdogs
 */
typedef enum CC2640R2DK_4XS_WatchdogName {
    CC2640R2DK_4XS_WATCHDOG0 = 0,

    CC2640R2DK_4XS_WATCHDOGCOUNT
} CC2640R2DK_4XS_WatchdogName;

/*!
 *  @def    CC2650_LAUNCHXL_TRNGName
 *  @brief  Enum of TRNG names on the board
 */
typedef enum CC2640R2DK_4XS_TRNGName {
    CC2640R2DK_4XS_TRNG0 = 0,
    CC2640R2DK_4XS_TRNGCOUNT
} CC2640R2DK_4XS_TRNGName;

#ifdef __cplusplus
}
#endif

//#endif /* __CC2640R2DK_4XS_BOARD_H__ */
