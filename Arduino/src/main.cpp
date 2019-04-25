#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_AMG88xx.h>

Adafruit_AMG88xx amg;

const int LedPin = 13;  
int ledState = 1;  
boolean dataTransferComplete = false;

float pixels[AMG88xx_PIXEL_ARRAY_SIZE];

void sendSerialData();
void writeIntAsBinary(int value);

void setup()  
{  
    bool status; 
    ledState = 1; 
    pinMode(LedPin, OUTPUT); 
    digitalWrite(LedPin, ledState); 

    Serial.begin(115200); 
    
    // default settings
    status = amg.begin();
    if (!status) {
        Serial.println("Could not find a valid AMG88xx sensor, check wiring!");
        while (1);
    } 
    Serial.println("-- Pixels Test --");
    Serial.println(); 

    delay(100); // let sensor boot up  
}  
  
void loop()  {   
    amg.readPixels(pixels);
    sendSerialData();  

    //delay a second
    delay(2000);     
}

void sendSerialData() {
    static boolean sendInProgress = false;
    static boolean first = true;
    char startMarker = '1'; //ascii symbol for 1 
    char dataReceived;
    uint16_t pixelInt;
    byte highBytes[64], lowBytes[64];
    byte byteArray[2][64];
    
//    //while 
    while (Serial.available() > 0 && dataTransferComplete == false) { 
  
       dataReceived = Serial.read();   
    
        if (dataReceived == startMarker) {
            ledState = 1;  
            digitalWrite(LedPin, ledState);
            for(int i=0; i < AMG88xx_PIXEL_ARRAY_SIZE; i++){
                //convert from float to int, will need to convert back once host receives
                pixelInt = uint16_t(pixels[i] * 100);
                highBytes[i] = pixelInt / 256;
                lowBytes[i] = pixelInt % 256;
            }  
            Serial.write(highBytes,64);
            Serial.write(lowBytes,64);
            //Serial.flush();
            dataTransferComplete == true;
            ledState = 0;  
            digitalWrite(LedPin, ledState);
        }
    }
}


