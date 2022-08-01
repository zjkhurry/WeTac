/*
 * ADSCommand.h
 *
 *  Created on: 2020Äê12ÔÂ30ÈÕ
 *      Author: jingkzhou3
 */

#ifndef APPLICATION_MUXCOMMAND_H_
#define APPLICATION_MUXCOMMAND_H_

//#include "board.h"
#include <ti/drivers/SPI.h>
#include <ti/drivers/PIN.h>
// constants define pins on CC2640

#define SPI0_MISO                     PIN_UNASSIGNED//IOID_2             /* P1.20 */
#define SPI0_MOSI                     IOID_6             /* P1.18 */
#define SPI0_CLK                      IOID_7             /* P1.16 */
#define SPI0_CSN                      IOID_8//PIN_UNASSIGNED
//#define SPI0_DRDY                     IOID_1
#define SPI0_START                    IOID_9


void MUX_Init();
void flashSelect(void);
void flashDeSelect(void);
void set_start(void);
void set_stop(void);
SPI_Handle spi_open(uint32_t bitRate);
bool spi_write(uint8_t *buf, size_t len);
bool spi_read(uint8_t *buf, size_t len);
void MUXWreg(uint8_t *reg);



#endif /* APPLICATION_MUXCOMMAND_H_ */
