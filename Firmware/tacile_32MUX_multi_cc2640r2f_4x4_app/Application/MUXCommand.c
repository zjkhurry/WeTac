/*
 * MUXCommand.c
 *
 *  Created on: 2020Äê12ÔÂ30ÈÕ
 *      Author: jingkzhou3
 */

/**
 * send commands for HV2801 chips.
 *
 * Copyright (c) 2013 by Adam Feuer <adam@adamfeuer.com>
 *
 * This library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this library.  If not, see <http://www.gnu.org/licenses/>.
 *
 */

#include "CC2640R2DK_4XS.h"
#include "MUXCommand.h"
#include <ti/drivers/SPI.h>
#include <ti/drivers/spi/SPICC26XXDMA.h>
#include <ti/drivers/dma/UDMACC26XX.h>
#include <ti/drivers/PIN.h>
#include <ti/drivers/GPIO.h>
#include <ti/display/Display.h>
#include <ti/sysbios/knl/Clock.h>
#include <ti/sysbios/knl/Task.h>

#define THREADSTACKSIZE (1024)
#define SPI_MSG_LENGTH  (32)

unsigned char masterRxBuffer[SPI_MSG_LENGTH];
unsigned char masterTxBuffer[SPI_MSG_LENGTH];

SPI_Handle      masterSpi;
static PIN_State  SPI_Pins;
static PIN_Handle SPI_HPins = NULL;

PIN_Config HV2801_PinsCfg[] =
{
//    SPI0_MISO | PIN_INPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    SPI0_MOSI | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    SPI0_CLK | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
    SPI0_CSN | PIN_GPIO_OUTPUT_EN | PIN_GPIO_HIGH | PIN_PUSHPULL | PIN_DRVSTR_MAX,
//    SPI0_DRDY | PIN_INPUT_EN | PIN_PULLUP | PIN_IRQ_NEGEDGE,
    SPI0_START | PIN_GPIO_OUTPUT_EN | PIN_GPIO_LOW | PIN_PUSHPULL | PIN_DRVSTR_MAX,
    PIN_TERMINATE
};

void MUX_Init(){
    SPI_HPins = PIN_open(&SPI_Pins, HV2801_PinsCfg);
}

void flashSelect(void){
    PIN_setOutputValue(SPI_HPins,SPI0_CSN,CC2640R2DK_4XS_FLASH_CS_ON);
//    GPIO_write(CC2640R2DK_4XS_SPI0_CS, CC2640R2DK_4XS_FLASH_CS_ON);
}

void flashDeSelect(void){
    PIN_setOutputValue(SPI_HPins,SPI0_CSN,CC2640R2DK_4XS_FLASH_CS_OFF);
//    GPIO_write(CC2640R2DK_4XS_SPI0_CS, CC2640R2DK_4XS_FLASH_CS_OFF);
}

void set_start(void){
    PIN_setOutputValue(SPI_HPins,SPI0_START,0);
}

void set_stop(void){
    PIN_setOutputValue(SPI_HPins,SPI0_START,1);
}

SPI_Handle spi_open(uint32_t bitRate){
    SPI_Params      spiParams;
    /* Open SPI as master (default) */
    SPI_init();
    SPI_Params_init(&spiParams);
    spiParams.frameFormat = SPI_POL0_PHA1;
    spiParams.mode = SPI_MASTER;
    spiParams.bitRate = bitRate;//4000000;
    spiParams.transferMode = SPI_MODE_BLOCKING;//SPI_MODE_CALLBACK;
    masterSpi = SPI_open(CC2640R2DK_4XS_SPI0, &spiParams);

    MUX_Init();
//    GPIO_setConfig(IOID_5, GPIO_CFG_OUTPUT | GPIO_CFG_OUT_HIGH);

    return masterSpi;
}

bool spi_write(uint8_t *buf, size_t len){
    SPI_Transaction masterTransaction;

    masterTransaction.count = len;
    masterTransaction.txBuf = buf;
    masterTransaction.arg = NULL;
    masterTransaction.rxBuf = NULL;
    return SPI_transfer(masterSpi, &masterTransaction) ? 1 : 0;
}

bool spi_read(uint8_t *buf, size_t len) {
    SPI_Transaction masterTransaction;

    masterTransaction.count = len;
    masterTransaction.rxBuf = buf;
    masterTransaction.txBuf = NULL;
    masterTransaction.arg = NULL;

    return SPI_transfer(masterSpi, &masterTransaction) ? 1 : 0;
}

void MUXWreg(uint8_t *reg) {
    bool flag;
    flashSelect();
    flag = spi_write(reg, 5);
//    if(!flag) {
//        while(1);
//    }
    flashDeSelect();
}



