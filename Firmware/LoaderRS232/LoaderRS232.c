/*
 * LoaderRS232.c
 *
 * Created: 7/19/2016 10:23:09 AM
 *  Author: Renan
 */ 


#include <avr/io.h>
#include <avr/boot.h>

#include "Bits.h"
#include "LoaderRS232.h"


uint8_t cmd;
address_t adr;
uint16_t data;

BOOTLOADER_SECTION
int main(void)
{	
	/////////////////////////////
	// Uart Init
	
	UCSRA = 0x02;
	UCSRB = (1<<TXEN) | (1<<RXEN);
	UCSRC = (3<<UCSZ0);
	
	UBRRH = 0;
	UBRRL = (BootUBRR);
	
	UART_Write(LOADERIDH);
	UART_Write(LOADERIDL);
	
	adr = 0;
	cmd = 0;
	
	// Waiting for Host 
	for(; adr < 60000;adr++ ){ 
			__asm("nop");

		if( UART_DataIsReady()) {
			cmd = UART_Read();
			if( cmd == PROG){
				UART_Write(CHECKBYTE);						// send checkbyte, identificating device
				
				for(char i=0; i<200; i++)					// wait a little to send page size and flash size
					__asm("nop");
					
				UART_Write(SPM_PAGESIZE/8);					// page size
				UART_Write(FLASH_SIZE/(SPM_PAGESIZE*8));	// chip size in pages
				
				break;
			}
		}
	}
	
	while( cmd == PROG ){
		if( UART_DataIsReady() ){
			// Cmd
			cmd = UART_Read();
			if( cmd == PROG ){
				// Page Address
				while(!UART_DataIsReady());
				adr = UART_Read();
				
#if FLASH_SIZE <= 65536
				adr <<= 8;
				while(!UART_DataIsReady());
				adr |= UART_Read();
#elif FLASH_SIZE > 65536
				for(char i=0; i<3; i++){	
					adr <<= 8;
					while(!UART_DataIsReady());
					adr |= UART_Read();
				}
#endif
				// fill the page buffer 
				for( uint16_t i=0; i<SPM_PAGESIZE; i+=2){
					// Data
					while(!UART_DataIsReady());
					data = UART_Read();
					data <<= 8;
					while(!UART_DataIsReady());
					data |= UART_Read();
					
					boot_page_fill(adr+i, data);
				}
				
				// write program to flash
				boot_page_erase(adr);
				boot_spm_busy_wait();
			
				boot_page_write(adr);
				boot_spm_busy_wait();
			
				UART_Write(ACK);
			}
		}
	}
	
	// Exit BootLoader - Go to program
	RESET();
}