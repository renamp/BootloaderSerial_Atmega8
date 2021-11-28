/*
 * LoaderRS232.h
 *
 * Created: 01/03/2017 14:56:49
 *  Author: Renan
 */ 


#ifndef LOADERRS232_H_
#define LOADERRS232_H_

#if defined (__AVR_ATmega8A__)
	#define FLASH_SIZE 8192
	typedef uint16_t address_t;
#elif defined (__AVR_ATmega2560__)
	#define FLASH_SIZE 262144
	typedef uint32_t address_t;
	#define UDR UDR2
	#define UCSZ0 UCSZ20
	#define UCSRA UCSR2A
	#define UCSRB UCSR2B
	#define UCSRC UCSR2C
	#define UBRRL UBRR2L
	#define UBRRH UBRR2H
	#define TXEN TXEN2
	#define RXEN RXEN2
	#define RXC RXC2
	#define UDRE UDR2
#endif

// define os bytes de referencia para o Loader SOftware
#define LOADERIDH	0xE6
#define LOADERIDL	0xA5

//define o byte de resposta do software
#define CHECKBYTE	0xAE

// instruçoes de controle
#define PROG	0xF1
#define ACK		0xF5
#define EXIT	0xF8


// define Baund Rate
#define BootBAUD 1000000

// calculete the value of UBRR
#define BootUBRR (((F_CPU/8)/BootBAUD)-1)

// Write data in the Buffer to Send
#define UART_Write(Byte) UDR=Byte

// Check if data is ready in Buffer
#define UART_DataIsReady() (UCSRA&(1<<RXC))

// Read data from buffer
#define UART_Read() UDR

#define UART_Waitbuffer() while(!(UCSRA&(1<<UDRE)))

#define RESET()  UCSRB=0;\
asm("ldi r30, 0");\
asm("ldi r31, 0");\
asm("ijmp");


#endif /* LOADERRS232_H_ */