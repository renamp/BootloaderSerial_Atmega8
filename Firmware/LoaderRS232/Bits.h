////////////////////////////////////////////////
//
// Manipulacao de Bits
//
////////////////////////////////////////////////

#define setbit(port, pin) port|=(1<<pin)
#define clearbit(port, pin) port&=~(1<<pin)
#define testbit(port, pin) (port&(1<<pin))
