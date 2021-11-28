//#define DEBUGON 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;

using System.Threading;

namespace LoaderRS232
{
    class Program
    {
        static int []flash = null;
        static SerialPort com = null;

        static int Loadersize;

        const byte LOADERIDH	=0xE6;              // Bytes sent from device at start bootloader
        const byte LOADERIDL	=0xA5;

        const byte CHECKBYTE    =0xAE;              // checkbyte byte to send to Device

        const byte PROG	    =0xF1;                  // Byte to Program one page
        const byte ACK		=0xF5;                  // Byte sent from Device indicating page was written
        const byte EXIT	    =0xF8;                  // Byte to exit bootLoader


        static bool deviceReq   = false;
        static bool deviceOn    = false;
        static bool deviceOk    = false;
        static bool deviceAck   = false;
        static bool findDevice  = false;
        static int pagesize    = 0;
        static int flashsize   = 0;

        static void Main(string[] args)
        {
            bool exitAfterProgram = false;
            string fileName = "";
            ////////////////////////////////
            // Com Config
            string comName  = "COM1";
            int comBaud     = 1000000;
            ////////////////////////////////


            if( args.Length == 0 ){
                System.Console.Write("\n Modo de Uso: LoaderRS232 [param] [hex file]\n For Help use param: -help \n");
            }
            else if( args.Length > 0 ){                
                for( int i=0; i<args.Length; i++){
                    if (args[i].Contains("-r")) exitAfterProgram = true;
                    else if (args[i].ToUpper().Contains("-COM")) comName = args[i].Substring(1).ToUpper();
                    else if (args[i].ToUpper().Contains("-BAUD=")) comBaud = int.Parse(args[i].Substring(6));
                    else if (args[i].ToUpper().Contains("-HELP")) printHelp();
                    else if (i==args.Length-1 ) fileName = args[i];
                }

                try{
                    // connect to the com port
                    com = new SerialPort(comName, comBaud);
                    com.DataReceived += new SerialDataReceivedEventHandler(com_DataReceived);
                    com.Open();

                    // Reset Device if Serial has DTR
                    com.DtrEnable = true;      // reset device
                    Thread.Sleep(100);
                    com.DiscardInBuffer();
                    findDevice = true;
                    com.DtrEnable = false;       // Leave device start
                    

                    // Wait for Device to connect
                    System.Console.WriteLine("Waiting Device..");
                    while (deviceOn == false || deviceOk || pagesize==0 || flashsize==0) ;      // device is conected

                    // set loader size base on flash size
                    if ( flashsize <= 65536 )
                        Loadersize = 512;
                    else if (flashsize > 65536 )
                        Loadersize = 8192;

                    // check the hex file
                    if (readFile(fileName) == false) System.Console.WriteLine(" Erro: File not Supported! ");
                        
                    //////////////////////////////////////////////////////
                    // Program Flahs
                    else {
                        System.Console.WriteLine("Writing..");
                        int adr = 0;
                        byte[] buff = { 0, 0, 0 };
                        
                        Console.Write( (adr*100/(flashsize-Loadersize)) + "%");

                        for( adr=0; adr<flashsize-Loadersize; adr+=pagesize ){
                            bool bytetoread = false;
                            for (int i = 0; i < pagesize; i++) if (flash[adr + i] != 0xff) bytetoread = true;
                            Console.Write("\b\b\b");
                            Console.Write((adr * 100 / (flashsize - Loadersize)) + "%");

                            if( bytetoread ){
                                buff[0] = PROG;
                                buff[1] = (byte)(adr >> 8);
                                buff[2] = (byte)(adr);
                                com.Write(buff, 0, 3); 
                                for (int i = 0; i < pagesize; i++){
                                    buff[0] = (byte)flash[adr+i];
                                    com.Write(buff, 0, 1);
                                }
                                while (deviceAck == false) ;
                                deviceAck = false;
                            }
                        }
                        if( exitAfterProgram ) {
                            buff[0] = EXIT;
                            com.Write(buff, 0, 1);
                        }

                        Console.Write("\b\b\b100%\n");
                        System.Console.WriteLine("Finished!");
                    }
                }
                catch{ System.Console.WriteLine(comName + " not Connected!!"); }
            }
            System.Console.Read();
        }


        static void com_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // verifica para conectar com o Dispositivo
            //if (deviceOn == false && com.BytesToRead >= 2){
            if( deviceOn == false ){
            ReadBuffer:                
                byte byteIn = (byte)com.ReadByte();
                if (findDevice && deviceReq == false && byteIn == LOADERIDH)
                    deviceReq = true;
                else if (deviceReq && byteIn == LOADERIDL)
                {
                    byte[] buff = new byte[2];
                    //com.Read(buff, 0, 2);
                    //if( buff[0] == LOADERIDH && buff[1] == LOADERIDL){
                    buff[0] = PROG;
                    deviceOn = true;
                    com.Write(buff, 0, 1);
                }
                else
                {
                    deviceReq = false;
                    if (com.BytesToRead > 0)    // try again if buffer is not empyt
                        goto ReadBuffer;
                }
            }
                // Verifica para receber pagesize and flashsize
            else if( deviceOn && pagesize==0 && com.BytesToRead>2 ){
                byte byteIn = (byte)com.ReadByte();
                if( byteIn == CHECKBYTE ){
                    pagesize = (int)com.ReadByte() * 8;
                    flashsize = (int)(com.ReadByte() * pagesize * 8);
                    System.Console.WriteLine("Device: Page_size=" +pagesize + " Flash_size=" + flashsize);
                    deviceOk = true;
                }
                else {
                    deviceOn = false;
                    Console.WriteLine("Erro comunication");
                    Console.WriteLine("Read: " + byteIn.ToString("X"));
                    Console.WriteLine(com.ReadByte().ToString("X"));
                }
            }

            else if( deviceOn && pagesize>0 && flashsize>0 && deviceOk ){
                if (com.ReadByte() == ACK) deviceAck = true;
            }

        }

        static int toHex(string str)
        {
            int hex=0;
            for( int i=0; i< str.Length; i++){
                hex *= 16;
                if (str[i] >= '0' && str[i] <= '9') hex |= (int)(str[i] - '0');
                else if (str[i] >= 'A' && str[i] <= 'F') hex |= (int)((str[i] - 'A') + 10);
                else if (str[i] >= 'a' && str[i] <= 'f') hex |= (int)((str[i] - 'a') + 10);
            }
            return hex;
        }

        static bool readFile(string filename){
            StreamReader file;
            try
            {
                flash = new int[flashsize];
                for (int i = 0; i < flashsize; i++) flash[i] = 0xff;
                
                file = new StreamReader(filename);

                int extendAdr = 0;
                bool loop = true;
                while (loop)
                {
                    char c = (char)file.Read();
                    if (c == ':')
                    {
                        // read num of bytes
                        int length = toHex((char)file.Read() + "" + (char)file.Read());
                        int adri = toHex((char)file.Read() + "" + (char)file.Read() + "" + (char)file.Read() + "" + (char)file.Read());
                        int cod = toHex((char)file.Read() + "" + (char)file.Read());

                        adri += extendAdr;

#if DEBUGON
                        System.Console.Write(length + "," + adri + "," + cod + ":");
#endif
                        // if everything okay..
                        if (length == 0 && adri == 0 && cod == 1) return true;

                        // if extended addressing
                        else if (cod == 2)
                        {
                            extendAdr = toHex((char)file.Read() + "" + (char)file.Read() + "" + (char)file.Read() + "" + (char)file.Read());
                            int checksum = toHex((char)file.Read() + "" + (char)file.Read());
                            extendAdr = extendAdr << 4;
                        }

                        else if( cod == 0 )
                        {
                            for (int i = 0; i < length; i += 2)
                            {
                                flash[adri + i + 1] = toHex((char)file.Read() + "" + (char)file.Read());
                                flash[adri + i] = toHex((char)file.Read() + "" + (char)file.Read());

#if DEBUGON
                            System.Console.Write(flash[adri + i] + " " + flash[adri+i+1])+", "; 
#endif
                            }
                            int checksum = toHex((char)file.Read() + "" + (char)file.Read());
#if DEBUGON
                            System.Console.WriteLine("");
#endif
                        }
                    }
                    else if (file.EndOfStream) loop = false;
                }                
            }
            catch { System.Console.WriteLine("Syntax Error\n"); }
            return false;
        }

        static void printHelp(){
            System.Console.WriteLine("LOADER RS232 Help Info..");
            System.Console.WriteLine("Uso: LoaderRS232 [params] [hex file]");
            System.Console.WriteLine("  [-r]        : Exit Loader after Flash Program");
            System.Console.WriteLine("  [-comN]     : (N is a number)   default: -com1 ");
            System.Console.WriteLine("  [-baud=N]   : (N is Baud speed) default: -baud=9600");
            System.Console.WriteLine("  [-help]     : Show this Help");
        }
    }

}
