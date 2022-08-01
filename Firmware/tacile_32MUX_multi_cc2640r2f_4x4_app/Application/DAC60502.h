/*
 * DAC60502.h
 *
 *  Created on: 2021Äê7ÔÂ9ÈÕ
 *      Author: jingkzhou3
 */

#ifndef APPLICATION_DAC60502_H_
#define APPLICATION_DAC60502_H_


#include <stdint.h>
#include <stdbool.h>
#include <ti/drivers/I2C.h>
#include <ti/drivers/SPI.h>

#define SPI0_SYNC IOID_1

#define SPI_Mode 0
#define I2C_Mode 1
#define I2C_SPEED_STANDARD        I2C_100kHz
#define I2C_SPEED_FAST            I2C_400kHz

//Define the size of the I2C buffer
 //I2C_BUFFER_LENGTH
#define I2C_BUFFER_LENGTH 32

//DAC60502 Address Byte (datasheet p27)
#define DAC60502_addr_AGND 0x48
#define DAC60502_addr_VDD 0x49
#define DAC60502_addr_SDA 0x4A
#define DAC60502_addr_SCL 0x4B

//Command byte
#define NOOP 0x00
#define DEVID 0x01
#define SYNC 0x02
#define CONFIG 0x03
#define GAIN 0x04
#define TRIGGER 0x05
#define BRDCAST 0x06
#define STATUS 0x07
#define DACA_DATA 0x08
#define DACB_DATA 0x09

#define RESOLUTION 0x2214
#define ADCA_PWDWN 0x0001
#define ADCB_PWDWN 0x0002
#define BUFFA_GAIN 0x0001
#define BUFFB_GAIN 0x0002
#define SOFT_RESET 0x000A
#define REF_DIV 0x0100

bool DAC60502_begin(bool mode);
bool i2c_write(unsigned char slave_addr, unsigned char reg_addr,
          unsigned char length, unsigned char const *data);
bool i2c_read(unsigned char slave_addr, unsigned char reg_addr,
      unsigned char length, unsigned char *data);
void write_reg(uint8_t slave_addr, uint8_t reg_addr, uint8_t length,
               uint8_t *data);
void read_reg(uint8_t slave_addr, uint8_t reg_addr, uint8_t length,
               uint8_t *data);
void set_DAC_A(uint8_t *dac);
void set_DAC_B(uint8_t *dac);


#endif /* APPLICATION_DAC60502_H_ */
