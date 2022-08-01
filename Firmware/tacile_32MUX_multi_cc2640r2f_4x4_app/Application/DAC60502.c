/*
 * DAC60502.c
 *
 *  Created on: 2021Äê7ÔÂ9ÈÕ
 *      Author: jingkzhou3
 */

#include <stdint.h>
#include <stdbool.h>
#include <ti/drivers/I2C.h>
#include "DAC60502.h"
#include "MUXCommand.h"
#include "CC2640R2DK_4XS.h"

static PIN_State  SPI_Pins;
static PIN_Handle SPI_HPins = NULL;
bool mode = 0;

PIN_Config DAC60502_PinsCfg[] =
{
    SPI0_SYNC | PIN_GPIO_OUTPUT_EN | PIN_GPIO_HIGH | PIN_PUSHPULL | PIN_DRVSTR_MAX,
    PIN_TERMINATE
};
I2C_Handle      i2c;
I2C_Transaction i2cTransaction;
uint8_t _i2caddr;
uint8_t         txBuffer[I2C_BUFFER_LENGTH];
uint8_t         rxBuffer[I2C_BUFFER_LENGTH];


bool DAC60502_begin(bool m) {
    mode = m;
    if(mode == I2C_Mode)
    {
        I2C_Params      i2cParams;
        /* Open I2C as master (default) */
        I2C_init();
        /* Create I2C for usage */
        I2C_Params_init(&i2cParams);
        i2cParams.bitRate = I2C_SPEED_FAST;
        i2c = I2C_open(CONFIG_I2C_0, &i2cParams);
    }
    else
    {
        SPI_HPins = PIN_open(&SPI_Pins, DAC60502_PinsCfg);
        //getSpiHandle();
    }

    //Set REF
    uint8_t data[2];
    data[1] = 0x03;
    data[0] = REF_DIV>>8;
    write_reg(DAC60502_addr_VDD, GAIN, 2, data);

  return true;
}

bool i2c_write(unsigned char slave_addr, unsigned char reg_addr,
          unsigned char length, unsigned char const *data)
{
    uint8_t commd[I2C_BUFFER_LENGTH];
    commd[0] = reg_addr;
    uint8_t i;
    for(i=0;i<length;i++)
    {
        commd[i+1] = data[i];
    }
    i2cTransaction.writeBuf   = commd;
    i2cTransaction.writeCount = length+1;
    i2cTransaction.readBuf    = rxBuffer;
    i2cTransaction.readCount  = 0;

    i2cTransaction.slaveAddress = slave_addr;

    return !I2C_transfer(i2c, &i2cTransaction);
}

bool i2c_read(unsigned char slave_addr, unsigned char reg_addr,
      unsigned char length, unsigned char *data)
{
    i2cTransaction.slaveAddress = slave_addr;
    i2cTransaction.writeBuf   = &reg_addr;
    i2cTransaction.writeCount = 1;
    i2cTransaction.readBuf    = data;
    i2cTransaction.readCount  = length;
    return !I2C_transfer(i2c, &i2cTransaction);
}

void write_reg(uint8_t slave_addr, uint8_t reg_addr, uint8_t length,
               uint8_t *data)
{
    if(mode)
        i2c_write(slave_addr, reg_addr, length, data);
    else
    {
        PIN_setOutputValue(SPI_HPins,SPI0_SYNC,0);
        spi_write(&reg_addr, 1);
        spi_write(data, length);
        PIN_setOutputValue(SPI_HPins,SPI0_SYNC,1);
    }
}

void read_reg(uint8_t slave_addr, uint8_t reg_addr, uint8_t length,
               uint8_t *data)
{
    if(mode)
        i2c_read(slave_addr, reg_addr, length, data);
    else
    {
        PIN_setOutputValue(SPI_HPins,SPI0_SYNC,0);
        uint8_t addr = reg_addr|0x80;
        spi_write(&addr, 1);
        spi_read(data, length);
        PIN_setOutputValue(SPI_HPins,SPI0_SYNC,1);
    }
}

void set_DAC_A(uint8_t *dac)
{
    write_reg(DAC60502_addr_VDD, DACA_DATA, 2, dac);
}

void set_DAC_B(uint8_t *dac)
{
    write_reg(DAC60502_addr_VDD, DACB_DATA, 2, dac);
}
