#include <Arduino.h>

const int LedPin = 13;  
int ledState = 0;  

const byte numBytes = 4;
byte bytesToSend[numBytes] = {0x33, 0x56, 0x56, 0x33};
byte numReceived = 0;

boolean dataSent = false;
boolean firstLoop = true;

void sendBytes();

void setup()  
{   
  pinMode(LedPin, OUTPUT);  
    
  Serial.begin(115200); 
  ledState = 1;   
}  
  
void loop()  
{   

    sendBytes();  
    digitalWrite(LedPin, ledState);   
        
    //delay(50);      
}

void sendBytes() {
    static boolean sendInProgress = false;
    int ndx = 0;
    char startMarker = '1'; //ascii symbol for 1 
    byte endMarker = 0x3E;
    char rb;
   
    while (Serial.available() > 0 && dataSent == false) {  
      
        rb = Serial.read(); 
        
        if (sendInProgress == true) {
            if (ndx < 1 ) {
                Serial.write(bytesToSend,4);
                Serial.write("\n");
                Serial.flush();
                ledState = 0;  
                digitalWrite(LedPin, ledState);  
               
               // receivedBytes[ndx] = rb;
                ndx++;
            }
            else {
                sendInProgress = false;
                numReceived = ndx;  // save the number for use when printing
                ndx = 0;
                dataSent = true;
            }
        }

        else if (rb == startMarker) {
            sendInProgress = true;  
        }
    }
}
